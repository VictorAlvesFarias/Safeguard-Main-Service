using Application.Dtos.Authenticator.Base;
using Application.Dtos.Default;
using Application.Services.AuthenticatorService;
using Domain.Entitites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Template.Controllers
{
    [Authorize]
    public class AuthenticatorController : Controller
    {
        public readonly IAuthenticatorService _authenticatorService;
        private readonly Controller _controller;

        public AuthenticatorController
        (
            IAuthenticatorService authenticatorService
        )
        {
            _authenticatorService = authenticatorService;
            _controller = this;
        }

        [HttpPost("create-authenticator")]
        public ActionResult<BaseResponse<Authenticator>> Register([FromBody] AuthenticatorRequest authenticatorRequest)
        {
            var result = _authenticatorService.RegisterAuthenticator(authenticatorRequest);
            return result.Result(_controller);
        }

        [HttpPut("edit-authenticator")]
        public ActionResult<BaseResponse<Authenticator>> Update([FromBody] AuthenticatorRequest authenticatorRequest, int id)
        {
            var result = _authenticatorService.UpdateAuthenticator(authenticatorRequest, id);
            return result.Result(_controller);
        }

        [HttpDelete("delete-authenticator")]
        public ActionResult<DefaultResponse> Delete(int id)
        {
            var result = _authenticatorService.DeleteAuthenticator(id);
            return result.DefaultResult(_controller);
        }

        [HttpGet("get-authenticators")]
        public ActionResult<BaseResponse<List<Authenticator>>> GetAll()
        {
            var result = _authenticatorService.GetAuthenticators();
            return result.Result(_controller);
        }

        [HttpGet("get-authenticator-by-id")]
        public ActionResult<BaseResponse<Authenticator>> GetById(int id)
        {
            var result = _authenticatorService.GetAuthenticatorById(id);
            return result.Result(_controller);
        }
    }
}
