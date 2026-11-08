namespace Domain.Entitites
{
    /// <summary>
    /// A site of a platform (e.g. "acesso.gov.br" for Gov, "steampowered.com" for Steam). Covers its
    /// subdomains too. One platform per domain for each user.
    /// </summary>
    public class PlatformDomain : BaseEntityUserRelation
    {
        public Platform Platform { get; set; }
        public int PlatformId { get; set; }
        public string Domain { get; set; }

        public void Create(int _platformId, string _domain)
        {
            PlatformId = _platformId;
            Domain = _domain;
            CreateDate = DateTime.Now;
            UpdateDate = DateTime.Now;
        }

        public void MoveTo(int _platformId)
        {
            PlatformId = _platformId;
            UpdateDate = DateTime.Now;
        }
    }
}
