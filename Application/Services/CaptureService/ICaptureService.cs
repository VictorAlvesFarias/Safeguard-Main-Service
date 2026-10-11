using Application.Dtos.Capture.Base;
using Application.Dtos.Default;
using Domain.Entitites;

namespace Application.Services.CaptureService
{
    public interface ICaptureService
    {
        BaseResponse<string> CreateCapture(CaptureRequest captureRequest);
        BaseResponse<CaptureSessionResponse> GetCapture(string id);
        BaseResponse<CaptureStatusResponse> GetCaptureStatus(string domain, string username, string? platform = null);
        BaseResponse<CaptureMatchResponse> GetCaptureMatch(string id);
        BaseResponse<CapturedCredential> CompleteCapture(CompleteCaptureRequest completeRequest);
        BaseResponse<CapturedCredential> IgnoreCapture(IgnoreCaptureRequest ignoreRequest);
        BaseResponse<List<CapturedCredential>> GetCapturedCredentials();
        DefaultResponse DeleteCapturedCredential(int id);
    }
}
