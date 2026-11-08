namespace Domain.Entitites
{
    public class Authenticator : BaseEntityUserRelation
    {
        public string Issuer { get; set; }
        public string AccountName { get; set; }
        public string Secret { get; set; }
        public string Algorithm { get; set; }
        public int Digits { get; set; }
        public int Period { get; set; }
        public Email? Email { get; set; }
        public int? EmailId { get; set; }
        /// <summary>E-mail address (mailbox) this code protects; may be set with or without an account.</summary>
        public EmailAddress? EmailAddress { get; set; }
        public int? EmailAddressId { get; set; }

        public void Create(string _issuer, string _accountName, string _secret, string _algorithm, int _digits, int _period, int? _emailId, int? _emailAddressId)
        {
            Issuer = _issuer;
            AccountName = _accountName;
            Secret = _secret;
            Algorithm = _algorithm;
            Digits = _digits;
            Period = _period;
            EmailId = _emailId;
            EmailAddressId = _emailAddressId;
            CreateDate = DateTime.Now;
            UpdateDate = DateTime.Now;
        }

        public void Update(string _issuer, string _accountName, string _secret, string _algorithm, int _digits, int _period, int? _emailId, int? _emailAddressId)
        {
            Issuer = _issuer;
            AccountName = _accountName;
            Secret = _secret;
            Algorithm = _algorithm;
            Digits = _digits;
            Period = _period;
            EmailId = _emailId;
            EmailAddressId = _emailAddressId;
            UpdateDate = DateTime.Now;
        }
    }
}
