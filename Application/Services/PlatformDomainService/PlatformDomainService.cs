using Application.Dtos.Default;
using Application.Services.CaptureService;
using Domain.Entitites;
using Infrastructure.Repositories.BaseRepository;

namespace Application.Services.PlatformDomainService
{
    public class PlatformDomainRequest
    {
        public int PlatformId { get; set; }
        public string Domain { get; set; }
    }

    public interface IPlatformDomainService
    {
        BaseResponse<List<PlatformDomain>> GetByPlatform(int platformId);
        BaseResponse<PlatformDomain> Add(PlatformDomainRequest request);
        DefaultResponse Remove(int id);

        /// <summary>The platform of a site: linked domains first, then old captures, then the name.</summary>
        Platform? ResolvePlatform(string? host, string? platformHint);

        /// <summary>
        /// Links the registrable domain of <paramref name="host"/> to the platform. A domain already on
        /// another platform moves only when <paramref name="move"/> (an explicit choice of the user).
        /// </summary>
        PlatformDomain? Link(string? host, int platformId, bool move);
    }

    public class PlatformDomainService : IPlatformDomainService
    {
        private readonly IBaseRepository<PlatformDomain> _domainRepository;
        private readonly IBaseRepository<Platform> _platformRepository;
        private readonly IBaseRepository<CapturedCredential> _capturedCredentialRepository;

        public PlatformDomainService
        (
            IBaseRepository<PlatformDomain> domainRepository,
            IBaseRepository<Platform> platformRepository,
            IBaseRepository<CapturedCredential> capturedCredentialRepository
        )
        {
            _domainRepository = domainRepository;
            _platformRepository = platformRepository;
            _capturedCredentialRepository = capturedCredentialRepository;
        }

        public BaseResponse<List<PlatformDomain>> GetByPlatform(int platformId)
        {
            return new BaseResponse<List<PlatformDomain>>
            {
                Data = _domainRepository.Get().Where(d => d.PlatformId == platformId).OrderBy(d => d.Domain).ToList(),
                Success = true,
            };
        }

        public BaseResponse<PlatformDomain> Add(PlatformDomainRequest request)
        {
            var response = new BaseResponse<PlatformDomain>();
            var domain = CaptureIdentity.NormalizeSiteDomain(request?.Domain);

            if (domain.Length == 0 || domain.Length > 253 || !domain.Contains('.') && domain != "localhost")
            {
                response.AddError("Informe um domínio, ex.: acesso.gov.br");
                return response;
            }
            if (!_platformRepository.Get().Any(p => p.Id == request!.PlatformId))
            {
                response.AddError("Plataforma não encontrada");
                return response;
            }

            var existing = _domainRepository.Get().FirstOrDefault(d => d.Domain == domain);
            if (existing is not null)
            {
                if (existing.PlatformId == request!.PlatformId)
                {
                    response.Data = existing;
                    response.Success = true;
                    return response;
                }
                var other = _platformRepository.Get().FirstOrDefault(p => p.Id == existing.PlatformId);
                response.AddError($"{domain} já está na plataforma {other?.Name ?? "outra"}");
                return response;
            }

            var created = new PlatformDomain();
            created.Create(request!.PlatformId, domain);
            response.Data = _domainRepository.AddAsync(created).Result;
            response.Success = true;
            return response;
        }

        public DefaultResponse Remove(int id)
        {
            var domain = _domainRepository.Get().FirstOrDefault(d => d.Id == id);
            if (domain is null)
            {
                var notFound = new DefaultResponse();
                notFound.AddError("Domínio não encontrado");
                return notFound;
            }
            return new DefaultResponse(_domainRepository.Remove(domain));
        }

        public Platform? ResolvePlatform(string? host, string? platformHint)
        {
            var site = CaptureIdentity.NormalizeSiteDomain(host);
            if (site.Length == 0)
            {
                return null;
            }

            var platforms = _platformRepository.Get().ToList();

            var linked = CaptureIdentity.MatchDomain(site, _domainRepository.Get().ToList());
            if (linked is not null && platforms.FirstOrDefault(p => p.Id == linked.PlatformId) is { } byDomain)
            {
                return byDomain;
            }

            // Captures saved before the domain table existed (kept as a fallback).
            var learnedPlatformId = _capturedCredentialRepository.Get()
                .Where(e => e.Domain == site && e.EmailId != null)
                .Select(e => e.Email!.PlatformId)
                .FirstOrDefault(id => id != null);

            return platforms.FirstOrDefault(p => p.Id == learnedPlatformId)
                ?? CaptureIdentity.MatchPlatform(site, platformHint, platforms);
        }

        public PlatformDomain? Link(string? host, int platformId, bool move)
        {
            var domain = CaptureIdentity.RegistrableDomain(host);
            if (domain.Length == 0 || !_platformRepository.Get().Any(p => p.Id == platformId))
            {
                return null;
            }

            var existing = _domainRepository.Get().FirstOrDefault(d => d.Domain == domain);
            if (existing is null)
            {
                var created = new PlatformDomain();
                created.Create(platformId, domain);
                return _domainRepository.AddAsync(created).Result;
            }

            if (existing.PlatformId != platformId && move)
            {
                existing.MoveTo(platformId);
                _domainRepository.Update(existing);
            }
            return existing;
        }
    }
}
