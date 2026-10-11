namespace Application.Dtos.Authenticator.Base
{
    public class AuthenticatorRequest
    {
        public string Issuer { get; set; }
        public string AccountName { get; set; }
        public string Secret { get; set; }
        public string Algorithm { get; set; }
        public int Digits { get; set; } = 6;
        public int Period { get; set; } = 30;
        public int? EmailId { get; set; }
    }
}
