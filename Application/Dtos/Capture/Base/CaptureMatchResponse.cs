namespace Application.Dtos.Capture.Base
{
    public class MatchedPlatform
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class MatchedProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Signature { get; set; } = "";
    }

    public class MatchedEmailAddress
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string? ProviderSignature { get; set; }
    }

    public class SimilarEmailAddress : MatchedEmailAddress
    {
        public int Distance { get; set; }
        public List<MatchedAccount> Accounts { get; set; } = new();
    }

    public class MatchedAccount
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public int? PlatformId { get; set; }
        public string? PlatformName { get; set; }
    }

    public class CaptureMatchResponse
    {
        public CaptureSessionResponse Capture { get; set; } = new();
        public string? Email { get; set; }
        public string? LocalPart { get; set; }
        public string? EmailDomain { get; set; }
        public MatchedPlatform? Platform { get; set; }
        public MatchedProvider? Provider { get; set; }
        public MatchedEmailAddress? EmailAddress { get; set; }
        public MatchedAccount? Account { get; set; }
        public List<MatchedAccount> OtherAccounts { get; set; } = new();
        public List<SimilarEmailAddress> SimilarAddresses { get; set; } = new();
    }
}
