using Application.Dtos.Capture.Base;
using Application.Dtos.Default;
using Domain.Entitites;
using Infrastructure.Repositories.BaseRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Application.Services.CaptureService
{
    public class CaptureService : ICaptureService
    {
        private static readonly TimeSpan CaptureLifetime = TimeSpan.FromMinutes(15);
        private const int MaxFieldLength = 512;
        private const int MaxUrlLength = 2048;

        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBaseRepository<CapturedCredential> _capturedCredentialRepository;
        private readonly IBaseRepository<Email> _emailRepository;
        private readonly IBaseRepository<Platform> _platformRepository;
        private readonly IBaseRepository<Provider> _providerRepository;
        private readonly IBaseRepository<EmailAddress> _emailAddressRepository;

        private class CaptureSession
        {
            public string UserId { get; set; } = "";
            public CaptureSessionResponse Data { get; set; } = new();
        }

        public CaptureService
        (
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor,
            IBaseRepository<CapturedCredential> capturedCredentialRepository,
            IBaseRepository<Email> emailRepository,
            IBaseRepository<Platform> platformRepository,
            IBaseRepository<Provider> providerRepository,
            IBaseRepository<EmailAddress> emailAddressRepository
        )
        {
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _capturedCredentialRepository = capturedCredentialRepository;
            _emailRepository = emailRepository;
            _platformRepository = platformRepository;
            _providerRepository = providerRepository;
            _emailAddressRepository = emailAddressRepository;
        }

        public BaseResponse<string> CreateCapture(CaptureRequest captureRequest)
        {
            var response = new BaseResponse<string>();
            var userId = CurrentUserId();
            var domain = NormalizeDomain(captureRequest.Domain);

            if (userId is null)
            {
                response.AddError("Usuário não identificado");
                return response;
            }

            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(captureRequest.Password))
            {
                response.AddError("A captura precisa do domínio e da senha");
                return response;
            }

            var id = Guid.NewGuid().ToString("N");
            var session = new CaptureSession
            {
                UserId = userId,
                Data = new CaptureSessionResponse
                {
                    Id = id,
                    CreatedAt = DateTime.Now,
                    Domain = Limit(domain, MaxFieldLength),
                    Url = Limit(captureRequest.Url, MaxUrlLength),
                    IconUrl = Limit(captureRequest.IconUrl, MaxUrlLength),
                    Platform = Limit(captureRequest.Platform, MaxFieldLength),
                    Username = Limit(captureRequest.Username?.Trim(), MaxFieldLength),
                    Password = Limit(captureRequest.Password, MaxFieldLength),
                    Phone = Limit(captureRequest.Phone, MaxFieldLength),
                    FirstName = Limit(captureRequest.FirstName, MaxFieldLength),
                    LastName = Limit(captureRequest.LastName, MaxFieldLength),
                }
            };

            _cache.Set(CacheKey(id), session, CaptureLifetime);

            response.Data = id;
            response.Success = true;

            return response;
        }

        public BaseResponse<CaptureSessionResponse> GetCapture(string id)
        {
            var response = new BaseResponse<CaptureSessionResponse>();
            var session = FindSession(id);

            if (session is null)
            {
                response.AddError("Captura não encontrada ou expirada");
                return response;
            }

            response.Data = session.Data;
            response.Success = true;

            return response;
        }

        public BaseResponse<CaptureStatusResponse> GetCaptureStatus(string domain, string username, string? platform = null)
        {
            var normalizedDomain = NormalizeDomain(domain);
            var normalizedUsername = NormalizeUsername(username);
            var record = _capturedCredentialRepository.Get()
                .FirstOrDefault(e => e.Domain == normalizedDomain && e.Username == normalizedUsername);
            var match = Resolve(normalizedDomain, platform, username);

            var response = new BaseResponse<CaptureStatusResponse>()
            {
                Data = new CaptureStatusResponse
                {
                    Saved = record?.Status == CapturedCredentialStatus.Saved || match.Account is not null,
                    Ignored = record?.Status == CapturedCredentialStatus.Ignored,
                    EmailId = record?.EmailId ?? match.Account?.Id,
                    PlatformName = match.Platform?.Name,
                },
                Success = true
            };

            return response;
        }

        public BaseResponse<CaptureMatchResponse> GetCaptureMatch(string id)
        {
            var response = new BaseResponse<CaptureMatchResponse>();
            var session = FindSession(id);

            if (session is null)
            {
                response.AddError("Captura não encontrada ou expirada");
                return response;
            }

            var match = Resolve(session.Data.Domain, session.Data.Platform, session.Data.Username);
            match.Capture = session.Data;

            response.Data = match;
            response.Success = true;

            return response;
        }

        public BaseResponse<CapturedCredential> CompleteCapture(CompleteCaptureRequest completeRequest)
        {
            var response = new BaseResponse<CapturedCredential>();
            var session = FindSession(completeRequest.Id);

            if (session is null)
            {
                response.AddError("Captura não encontrada ou expirada");
                return response;
            }

            var email = completeRequest.EmailId is null
                ? null
                : _emailRepository.Get().FirstOrDefault(e => e.Id == completeRequest.EmailId);

            if (completeRequest.EmailId is not null && email is null)
            {
                response.AddError("Conta não encontrada");
                return response;
            }

            response.Data = Upsert(
                NormalizeDomain(session.Data.Domain),
                NormalizeUsername(session.Data.Username),
                CapturedCredentialStatus.Saved,
                email?.Id
            );
            response.Success = true;

            _cache.Remove(CacheKey(session.Data.Id));

            return response;
        }

        public BaseResponse<CapturedCredential> IgnoreCapture(IgnoreCaptureRequest ignoreRequest)
        {
            var response = new BaseResponse<CapturedCredential>();
            var domain = NormalizeDomain(ignoreRequest.Domain);

            if (string.IsNullOrWhiteSpace(domain))
            {
                response.AddError("Informe o domínio");
                return response;
            }

            response.Data = Upsert(
                Limit(domain, MaxFieldLength)!,
                Limit(NormalizeUsername(ignoreRequest.Username), MaxFieldLength)!,
                CapturedCredentialStatus.Ignored,
                null
            );
            response.Success = true;

            return response;
        }

        public BaseResponse<List<CapturedCredential>> GetCapturedCredentials()
        {
            var records = _capturedCredentialRepository.Get()
                .Include(e => e.Email)
                .ThenInclude(e => e!.Platform)
                .OrderByDescending(e => e.UpdateDate)
                .ToList();

            var response = new BaseResponse<List<CapturedCredential>>()
            {
                Data = records,
                Success = true
            };

            return response;
        }

        public DefaultResponse DeleteCapturedCredential(int id)
        {
            var record = _capturedCredentialRepository.Get().FirstOrDefault(e => e.Id == id);

            if (record is null)
            {
                var notFound = new DefaultResponse();
                notFound.AddError("Registro não encontrado");
                return notFound;
            }

            var success = _capturedCredentialRepository.Remove(record);
            var response = new DefaultResponse(success);

            return response;
        }

        public static string NormalizeDomain(string? domain)
        {
            var value = (domain ?? "").Trim().ToLowerInvariant();
            return value.StartsWith("www.") ? value[4..] : value;
        }

        public static string NormalizeUsername(string? username) => (username ?? "").Trim().ToLowerInvariant();

        private CaptureMatchResponse Resolve(string? rawDomain, string? platformHint, string? username)
        {
            var domain = NormalizeDomain(rawDomain);
            var identity = (username ?? "").Trim();
            var (localPart, emailDomain) = CaptureIdentity.SplitEmail(identity);

            var platforms = _platformRepository.Get().ToList();
            var providers = _providerRepository.Get().ToList();
            var addresses = _emailAddressRepository.Get().Include(e => e.Provider).ToList();
            var accounts = _emailRepository.Get().Include(e => e.Platform).ToList();

            var platform = ResolvePlatform(domain, platformHint, platforms);
            var provider = CaptureIdentity.MatchProvider(emailDomain, providers);
            var address = CaptureIdentity.MatchAddress(localPart, provider, addresses);
            var sameIdentity = identity.Length == 0
                ? new List<Email>()
                : CaptureIdentity.SameIdentity(identity, localPart, address, accounts);
            var account = platform is null ? null : sameIdentity.FirstOrDefault(e => e.PlatformId == platform.Id);

            static MatchedAccount ToMatched(Email e) => new()
            {
                Id = e.Id,
                Username = e.Username,
                PlatformId = e.PlatformId,
                PlatformName = e.Platform?.Name,
            };

            return new CaptureMatchResponse
            {
                Email = localPart is null ? null : identity.ToLowerInvariant(),
                LocalPart = localPart,
                EmailDomain = emailDomain,
                Platform = platform is null ? null : new MatchedPlatform { Id = platform.Id, Name = platform.Name },
                Provider = provider is null ? null : new MatchedProvider { Id = provider.Id, Name = provider.Name, Signature = provider.Signature },
                EmailAddress = address is null ? null : new MatchedEmailAddress
                {
                    Id = address.Id,
                    Username = address.Username,
                    ProviderSignature = address.Provider?.Signature,
                },
                Account = account is null ? null : ToMatched(account),
                OtherAccounts = sameIdentity.Where(e => e != account).Select(ToMatched).ToList(),
                SimilarAddresses = address is not null
                    ? new List<SimilarEmailAddress>()
                    : CaptureIdentity.SimilarAddresses(localPart, provider, addresses)
                        .Select(similar => new SimilarEmailAddress
                        {
                            Id = similar.Id,
                            Username = similar.Username,
                            ProviderSignature = similar.Provider?.Signature,
                            Distance = CaptureIdentity.Distance(localPart!, similar.Username.Trim()),
                            Accounts = CaptureIdentity
                                .SameIdentity($"{similar.Username}@{similar.Provider?.Signature}", similar.Username.Trim().ToLowerInvariant(), similar, accounts)
                                .Select(ToMatched)
                                .ToList(),
                        })
                        .ToList(),
            };
        }

        private Platform? ResolvePlatform(string domain, string? platformHint, List<Platform> platforms)
        {
            var learnedPlatformId = _capturedCredentialRepository.Get()
                .Where(e => e.Domain == domain && e.EmailId != null)
                .Select(e => e.Email!.PlatformId)
                .FirstOrDefault(id => id != null);

            return platforms.FirstOrDefault(p => p.Id == learnedPlatformId)
                ?? CaptureIdentity.MatchPlatform(domain, platformHint, platforms);
        }

        private CapturedCredential Upsert(string domain, string username, CapturedCredentialStatus status, int? emailId)
        {
            var record = _capturedCredentialRepository.Get()
                .FirstOrDefault(e => e.Domain == domain && e.Username == username);

            if (record is null)
            {
                record = new CapturedCredential();
                record.Create(domain, username, status, emailId);
                return _capturedCredentialRepository.AddAsync(record).Result;
            }

            record.Update(status, emailId);
            _capturedCredentialRepository.Update(record);

            return record;
        }

        private CaptureSession? FindSession(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_cache.TryGetValue(CacheKey(id), out CaptureSession session))
            {
                return null;
            }

            return session.UserId == CurrentUserId() ? session : null;
        }

        private string? CurrentUserId() =>
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static string CacheKey(string id) => $"capture:{id}";

        private static string? Limit(string? value, int max) =>
            value is null ? null : value.Length > max ? value[..max] : value;
    }
}
