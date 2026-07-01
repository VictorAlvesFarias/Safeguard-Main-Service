using Application.Dtos;
using Application.Dtos.Default;
using Application.Services.AppFileService;
using Domain.Entitites;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Template.Controllers
{
    public class AppFileController : Controller
    {

        public readonly IAppFileService _appFileService;
        private readonly Controller _controller;

        public AppFileController
        (
            IAppFileService appFileService
        )
        {
            _appFileService = appFileService;
            _controller = this;   
        }

        [HttpPost("upload-file")]
        public ActionResult<BaseResponse<AppFile>> UploadFile([FromForm]  IFormFile image)
        {
            var result = _appFileService.InsertFile(image);
            return result.Result(_controller);
        }

        [HttpPost("upload-file-url")]
        public ActionResult<BaseResponse<AppFile>> UploadFile([FromBody] UploadFileFromUrlRequest req)
        {
            var result = _appFileService.InsertFileUrl(req.ImageUrl);
            return result.Result(_controller);
        }

        [HttpDelete("delete-file")]
        public ActionResult<DefaultResponse> DeleteFile(int id)
        {
            var result = _appFileService.DeleteFile(id);
            return result.DefaultResult(_controller);
        }
        
        [HttpGet("get-files")]
        public ActionResult<BaseResponse<List<AppFile>>> GetFiles()
        {
            var result = _appFileService.GetFiles();
            return result.Result(_controller);
        }
    }
}
