namespace Domain.Entitites
{
    public enum CapturedCredentialStatus
    {
        Saved = 1,
        Ignored = 2
    }

    public class CapturedCredential : BaseEntityUserRelation
    {
        public string Domain { get; set; }
        public string Username { get; set; }
        public CapturedCredentialStatus Status { get; set; }
        public Email? Email { get; set; }
        public int? EmailId { get; set; }

        public void Create(string _domain, string _username, CapturedCredentialStatus _status, int? _emailId)
        {
            Domain = _domain;
            Username = _username;
            Status = _status;
            EmailId = _emailId;
            CreateDate = DateTime.Now;
            UpdateDate = DateTime.Now;
        }

        public void Update(CapturedCredentialStatus _status, int? _emailId)
        {
            Status = _status;
            EmailId = _emailId ?? EmailId;
            UpdateDate = DateTime.Now;
        }
    }
}
