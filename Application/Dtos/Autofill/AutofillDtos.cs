namespace Application.Dtos.Autofill
{
    /// <summary>One login the extension can fill on a site.</summary>
    public class AutofillEntry
    {
        /// <summary>"account" (an Email row) or "address" (an e-mail address on the site's provider).</summary>
        public string Kind { get; set; }
        public int Id { get; set; }
        public string Username { get; set; }
        /// <summary>The account's e-mail address when it differs from Username: the user picks which one to sign in with.</summary>
        public string? Email { get; set; }
        public string? Platform { get; set; }
        public int? PlatformId { get; set; }
        public bool HasPassword { get; set; }
        public bool HasCode { get; set; }
    }

    public class AutofillEntriesResponse
    {
        public string Domain { get; set; }
        /// <summary>What a quick link from this page would add (acesso.gov.br for sso.acesso.gov.br).</summary>
        public string LinkDomain { get; set; }
        public string? Platform { get; set; }
        public List<AutofillEntry> Entries { get; set; } = new();
    }

    public class AutofillLinkRequest
    {
        public string Url { get; set; }
        public int PlatformId { get; set; }
    }

    /// <summary>What gets typed into the page. The authenticator secret never leaves the API.</summary>
    public class AutofillFillResponse
    {
        public string Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Code { get; set; }
        public int? CodeRemainingSeconds { get; set; }
    }
}
