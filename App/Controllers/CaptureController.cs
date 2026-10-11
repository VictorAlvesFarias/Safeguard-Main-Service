using Application.Dtos.Capture.Base;
using Application.Dtos.Default;
using Application.Services.CaptureService;
using Domain.Entitites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Template.Controllers
{
    [Authorize]
    public class CaptureController : Controller
    {
        public readonly ICaptureService _captureService;
        private readonly Controller _controller;

        public CaptureController
        (
            ICaptureService captureService
        )
        {
            _captureService = captureService;
            _controller = this;
        }

        [HttpPost("create-capture")]
        public ActionResult<BaseResponse<string>> CreateCapture([FromBody] CaptureRequest captureRequest)
        {
            var result = _captureService.CreateCapture(captureRequest);
            return result.Result(_controller);
        }

        [HttpGet("get-capture")]
        public ActionResult<BaseResponse<CaptureSessionResponse>> GetCapture(string id)
        {
            var result = _captureService.GetCapture(id);
            return result.Result(_controller);
        }

        [HttpGet("get-capture-status")]
        public ActionResult<BaseResponse<CaptureStatusResponse>> GetCaptureStatus(string domain, string username, string? platform)
        {
            var result = _captureService.GetCaptureStatus(domain, username, platform);
            return result.Result(_controller);
        }

        [HttpGet("get-capture-match")]
        public ActionResult<BaseResponse<CaptureMatchResponse>> GetCaptureMatch(string id)
        {
            var result = _captureService.GetCaptureMatch(id);
            return result.Result(_controller);
        }

        [HttpPost("complete-capture")]
        public ActionResult<BaseResponse<CapturedCredential>> CompleteCapture([FromBody] CompleteCaptureRequest completeRequest)
        {
            var result = _captureService.CompleteCapture(completeRequest);
            return result.Result(_controller);
        }

        [HttpPost("ignore-capture")]
        public ActionResult<BaseResponse<CapturedCredential>> IgnoreCapture([FromBody] IgnoreCaptureRequest ignoreRequest)
        {
            var result = _captureService.IgnoreCapture(ignoreRequest);
            return result.Result(_controller);
        }

        [HttpGet("get-captured-credentials")]
        public ActionResult<BaseResponse<List<CapturedCredential>>> GetCapturedCredentials()
        {
            var result = _captureService.GetCapturedCredentials();
            return result.Result(_controller);
        }

        [HttpDelete("delete-captured-credential")]
        public ActionResult<DefaultResponse> DeleteCapturedCredential(int id)
        {
            var result = _captureService.DeleteCapturedCredential(id);
            return result.DefaultResult(_controller);
        }
    }
}
