using Application.Dtos.Autofill;
using Application.Dtos.Default;
using Application.Services.AuthenticatorService;
using Application.Services.CaptureService;
using Application.Services.PlatformDomainService;
using Domain.Entitites;
using Infrastructure.Repositories.BaseRepository;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AutofillService
{
    public interface IAutofillService
    {
        BaseResponse<AutofillEntriesResponse> GetEntries(string url);
        BaseResponse<AutofillFillResponse> Fill(string url, string kind, int id);
        /// <summary>Accounts of every platform matching the text (no secrets), to pick one for an unknown site.</summary>
        BaseResponse<List<AutofillEntry>> Search(string? text);
        /// <summary>Links the site of the url to a platform (an explicit choice of the user).</summary>
        BaseResponse<string> Link(AutofillLinkRequest request);
        /// <summary>One account's values regardless of the site ("only this time", chosen by the user).</summary>
        BaseResponse<AutofillFillResponse> FillAccount(int id);
    }

    /// <summary>
    /// Logins the browser extension may fill on a site. The site is the tab's URL (given by the
    /// extension, not by the page): only accounts on the platform recognized for that site, and
    /// addresses of an e-mail provider whose own site it is, are offered or filled.
    /// </summary>
    public class AutofillService : IAutofillService
    {
        private readonly IBaseRepository<Email> _emailRepository;
        private readonly IBaseRepository<EmailAddress> _emailAddressRepository;
        private readonly IBaseRepository<Platform> _platformRepository;
        private readonly IBaseRepository<Provider> _providerRepository;
        private readonly IBaseRepository<Authenticator> _authenticatorRepository;
        private readonly IBaseRepository<CapturedCredential> _capturedCredentialRepository;
        private readonly IPlatformDomainService _platformDomainService;

        public AutofillService
        (
            IBaseRepository<Email> emailRepository,
            IBaseRepository<EmailAddress> emailAddressRepository,
            IBaseRepository<Platform> platformRepository,
            IBaseRepository<Provider> providerRepository,
            IBaseRepository<Authenticator> authenticatorRepository,
            IBaseRepository<CapturedCredential> capturedCredentialRepository,
            IPlatformDomainService platformDomainService
        )
        {
            _platformDomainService = platformDomainService;
            _emailRepository = emailRepository;
            _emailAddressRepository = emailAddressRepository;
            _platformRepository = platformRepository;
            _providerRepository = providerRepository;
            _authenticatorRepository = authenticatorRepository;
            _capturedCredentialRepository = capturedCredentialRepository;
        }

        private class SiteLogins
        {
            public string Domain = "";
            public Platform? Platform;
            public List<Email> Accounts = new();
            public List<EmailAddress> Addresses = new();
            public HashSet<int> SiteProviderIds = new();
            public List<Authenticator> Authenticators = new();
        }

        private static string HostOf(string? url)
        {
            var value = (url ?? "").Trim();
            var host = Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.Host : value;
            return CaptureService.CaptureService.NormalizeDomain(host);
        }

        private SiteLogins Resolve(string url)
        {
            var site = new SiteLogins { Domain = HostOf(url) };
            if (site.Domain.Length == 0)
            {
                return site;
            }

            var domainKey = CaptureIdentity.Normalize(CaptureIdentity.DomainKey(site.Domain));

            // Platform: linked domains first, then old captures, then the name.
            site.Platform = _platformDomainService.ResolvePlatform(site.Domain, null);

            // E-mail providers whose own site this is (e.g. accounts.google.com for Google / gmail.com).
            site.SiteProviderIds = _providerRepository.Get().ToList()
                .Where(p => domainKey.Length > 0 && (
                    CaptureIdentity.Normalize(p.Name) == domainKey ||
                    CaptureIdentity.Normalize(CaptureIdentity.DomainKey(p.Signature)) == domainKey))
                .Select(p => p.Id)
                .ToHashSet();

            if (site.Platform is not null)
            {
                site.Accounts = _emailRepository.Get()
                    .Include(e => e.Platform)
                    .Include(e => e.EmailAddress)
                    .ThenInclude(e => e.Provider)
                    .Where(e => e.PlatformId == site.Platform.Id)
                    .ToList();
            }

            if (site.SiteProviderIds.Count > 0)
            {
                // Addresses not already offered through an account of this site.
                site.Addresses = _emailAddressRepository.Get()
                    .Include(e => e.Provider)
                    .Where(e => site.SiteProviderIds.Contains(e.ProviderId))
                    .ToList()
                    .Where(a => !site.Accounts.Any(e =>
                        e.EmailAddressId == a.Id ||
                        string.Equals((e.Username ?? "").Trim(), AddressText(a), StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            site.Authenticators = _authenticatorRepository.Get().ToList();

            return site;
        }

        private static string AddressText(EmailAddress address) =>
            $"{address.Username?.Trim()}@{address.Provider?.Signature?.Trim()}";

        /// <summary>The account's e-mail address, only when it is not already its user name.</summary>
        private static string? EmailOf(Email account)
        {
            var email = account.EmailAddress is not null ? AddressText(account.EmailAddress) : null;
            return email is null || string.Equals(email, (account.Username ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                ? null
                : email;
        }

        /// <summary>
        /// The account's own code; for the login of the mailbox itself (the site is its provider's),
        /// the code linked to its e-mail address.
        /// </summary>
        private static Authenticator? CodeFor(Email account, SiteLogins site) =>
            site.Authenticators.FirstOrDefault(a => a.EmailId == account.Id)
            ?? (account.EmailAddress is not null && site.SiteProviderIds.Contains(account.EmailAddress.ProviderId)
                ? site.Authenticators.FirstOrDefault(a => a.EmailAddressId == account.EmailAddressId)
                : null);

        private static Authenticator? CodeFor(EmailAddress address, SiteLogins site) =>
            site.Authenticators.FirstOrDefault(a => a.EmailAddressId == address.Id);

        public BaseResponse<AutofillEntriesResponse> GetEntries(string url)
        {
            var site = Resolve(url);
            var response = new AutofillEntriesResponse
            {
                Domain = site.Domain,
                LinkDomain = CaptureIdentity.RegistrableDomain(site.Domain),
                Platform = site.Platform?.Name,
            };

            response.Entries.AddRange(site.Accounts.Select(e => new AutofillEntry
            {
                Kind = "account",
                Id = e.Id,
                Username = e.Username,
                Email = EmailOf(e),
                Platform = e.Platform?.Name,
                PlatformId = e.PlatformId,
                HasPassword = !string.IsNullOrEmpty(e.Password),
                HasCode = CodeFor(e, site) is not null,
            }));
            response.Entries.AddRange(site.Addresses.Select(a => new AutofillEntry
            {
                Kind = "address",
                Id = a.Id,
                Username = AddressText(a),
                Platform = a.Provider?.Name,
                HasPassword = false,
                HasCode = CodeFor(a, site) is not null,
            }));

            return new BaseResponse<AutofillEntriesResponse> { Data = response, Success = true };
        }

        public BaseResponse<List<AutofillEntry>> Search(string? text)
        {
            var term = CaptureIdentity.Normalize(text);
            var accounts = _emailRepository.Get()
                .Include(e => e.Platform)
                .Include(e => e.EmailAddress)
                .ThenInclude(e => e.Provider)
                .Where(e => e.PlatformId != null)
                .ToList()
                .Where(e => term.Length == 0
                    || CaptureIdentity.Normalize(e.Platform?.Name).Contains(term)
                    || CaptureIdentity.Normalize(e.Username).Contains(term))
                .OrderBy(e => e.Platform?.Name)
                .ThenBy(e => e.Username)
                .Take(30)
                .ToList();
            var authenticators = _authenticatorRepository.Get().ToList();

            return new BaseResponse<List<AutofillEntry>>
            {
                Data = accounts.Select(e => new AutofillEntry
                {
                    Kind = "account",
                    Id = e.Id,
                    Username = e.Username,
                    Email = EmailOf(e),
                    Platform = e.Platform?.Name,
                    PlatformId = e.PlatformId,
                    HasPassword = !string.IsNullOrEmpty(e.Password),
                    HasCode = authenticators.Any(a => a.EmailId == e.Id),
                }).ToList(),
                Success = true,
            };
        }

        public BaseResponse<string> Link(AutofillLinkRequest request)
        {
            var response = new BaseResponse<string>();
            var linked = _platformDomainService.Link(HostOf(request?.Url), request?.PlatformId ?? 0, move: true);
            if (linked is null)
            {
                response.AddError("Não foi possível atrelar este site.");
                return response;
            }
            response.Data = linked.Domain;
            response.Success = true;
            return response;
        }

        public BaseResponse<AutofillFillResponse> FillAccount(int id)
        {
            var response = new BaseResponse<AutofillFillResponse>();
            var account = _emailRepository.Get()
                .Include(e => e.EmailAddress)
                .ThenInclude(e => e.Provider)
                .FirstOrDefault(e => e.Id == id);
            if (account is null)
            {
                response.AddError("Conta não encontrada.");
                return response;
            }

            var fill = new AutofillFillResponse { Username = account.Username, Email = EmailOf(account), Password = account.Password };
            var code = _authenticatorRepository.Get().FirstOrDefault(a => a.EmailId == account.Id);
            if (code is not null)
            {
                (fill.Code, var remaining) = Totp.Generate(code.Secret, code.Algorithm, code.Digits, code.Period, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                fill.CodeRemainingSeconds = remaining;
            }

            response.Data = fill;
            response.Success = true;
            return response;
        }

        public BaseResponse<AutofillFillResponse> Fill(string url, string kind, int id)
        {
            var response = new BaseResponse<AutofillFillResponse>();
            var site = Resolve(url);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Authenticator? code;
            AutofillFillResponse fill;

            if (kind == "account" && site.Accounts.FirstOrDefault(e => e.Id == id) is { } account)
            {
                fill = new AutofillFillResponse { Username = account.Username, Email = EmailOf(account), Password = account.Password };
                code = CodeFor(account, site);
            }
            else if (kind == "address" && site.Addresses.FirstOrDefault(a => a.Id == id) is { } address)
            {
                fill = new AutofillFillResponse { Username = AddressText(address) };
                code = CodeFor(address, site);
            }
            else
            {
                response.AddError("Esta conta não é deste site.");
                return response;
            }

            if (code is not null)
            {
                (fill.Code, var remaining) = Totp.Generate(code.Secret, code.Algorithm, code.Digits, code.Period, now);
                fill.CodeRemainingSeconds = remaining;
            }

            response.Data = fill;
            response.Success = true;
            return response;
        }
    }
}
