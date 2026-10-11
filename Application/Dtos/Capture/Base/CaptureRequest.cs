namespace Application.Dtos.Capture.Base
{
    public class CaptureRequest
    {
        public string? Domain { get; set; }
        public string? Url { get; set; }
        public string? IconUrl { get; set; }
        public string? Platform { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class CaptureSessionResponse : CaptureRequest
    {
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class CaptureStatusResponse
    {
        public bool Saved { get; set; }
        public bool Ignored { get; set; }
        public int? EmailId { get; set; }
        public string? PlatformName { get; set; }
    }

    public class CompleteCaptureRequest
    {
        public string? Id { get; set; }
        public int? EmailId { get; set; }
    }

    public class IgnoreCaptureRequest
    {
        public string? Domain { get; set; }
        public string? Username { get; set; }
    }
}
