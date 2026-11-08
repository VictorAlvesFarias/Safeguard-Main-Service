using Application.Dtos.Default;
using Application.Services.PlatformDomainService;
using Domain.Entitites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Template.Controllers
{
    /// <summary>Sites (domains) of each platform, used to recognize which platform a login page is.</summary>
    [Authorize]
    public class PlatformDomainController : Controller
    {
        private readonly IPlatformDomainService _platformDomainService;

        public PlatformDomainController(IPlatformDomainService platformDomainService)
        {
            _platformDomainService = platformDomainService;
        }

        [HttpGet("get-platform-domains")]
        public ActionResult<BaseResponse<List<PlatformDomain>>> GetByPlatform(int platformId)
        {
            return _platformDomainService.GetByPlatform(platformId).Result(this);
        }

        [HttpPost("create-platform-domain")]
        public ActionResult<BaseResponse<PlatformDomain>> Add([FromBody] PlatformDomainRequest request)
        {
            return _platformDomainService.Add(request).Result(this);
        }

        [HttpDelete("delete-platform-domain")]
        public ActionResult<DefaultResponse> Remove(int id)
        {
            return _platformDomainService.Remove(id).DefaultResult(this);
        }
    }
}
