using Application.Services.CaptureService;
using Domain.Entitites;

namespace Test
{
    public class CaptureIdentityTests
    {
        private static Platform P(int id, string name) => new() { Id = id, Name = name };
        private static Provider Pr(int id, string name, string signature) => new() { Id = id, Name = name, Signature = signature };

        private static readonly List<Platform> Platforms = new()
        {
            P(1, "Facebook"), P(2, "Google"), P(3, "Barão de Mauá"), P(4, "Itaú"), P(5, "X"), P(6, "Instagram"),
        };

        [Theory]
        [InlineData("facebook.com", "facebook")]
        [InlineData("m.facebook.com", "facebook")]
        [InlineData("accounts.google.com", "google")]
        [InlineData("baraodemaua.com.br", "baraodemaua")]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("localhost", "localhost")]
        public void DomainKey_takes_the_name_part(string domain, string expected)
        {
            Assert.Equal(expected, CaptureIdentity.DomainKey(domain));
        }

        [Theory]
        [InlineData("facebook.com", null, 1)]
        [InlineData("web.facebook.com", null, 1)]
        [InlineData("accounts.google.com", null, 2)]
        [InlineData("baraodemaua.br", null, 3)]
        [InlineData("itau.com.br", null, 4)]
        [InlineData("login.instagram.com", "Instagram", 6)]
        public void MatchPlatform_finds_the_site_platform(string domain, string? hint, int expectedId)
        {
            Assert.Equal(expectedId, CaptureIdentity.MatchPlatform(domain, hint, Platforms)?.Id);
        }

        [Theory]
        [InlineData("xbox.com")]
        [InlineData("exemplo.com")]
        public void MatchPlatform_does_not_match_short_or_unrelated_names(string domain)
        {
            Assert.Null(CaptureIdentity.MatchPlatform(domain, null, Platforms));
        }

        [Fact]
        public void Provider_comes_from_the_email_domain()
        {
            var providers = new List<Provider> { Pr(1, "Google", "gmail.com"), Pr(2, "Hotmail", "hotmail.com") };
            var (localPart, domain) = CaptureIdentity.SplitEmail("Fulano@Gmail.com");

            Assert.Equal("fulano", localPart);
            Assert.Equal("gmail.com", domain);
            Assert.Equal(1, CaptureIdentity.MatchProvider(domain, providers)?.Id);
            Assert.Null(CaptureIdentity.MatchProvider(null, providers));
        }

        [Theory]
        [InlineData("drofxt", "drofxyt", 1)]
        [InlineData("fulano", "Fulano", 0)]
        [InlineData("victor.alves", "victor.alvez", 1)]
        [InlineData("abc", "xyz", 3)]
        public void Distance_counts_letter_edits(string a, string b, int expected)
        {
            Assert.Equal(expected, CaptureIdentity.Distance(a, b));
        }

        [Fact]
        public void SimilarAddresses_suggests_typos_on_the_same_provider()
        {
            var gmail = Pr(1, "Google", "gmail.com");
            var addresses = new List<EmailAddress>
            {
                new() { Id = 1, Username = "drofxyt", ProviderId = 1 },
                new() { Id = 2, Username = "drofxt", ProviderId = 2 },
                new() { Id = 3, Username = "victoralves", ProviderId = 1 },
            };

            Assert.Equal(new[] { 1 }, CaptureIdentity.SimilarAddresses("drofxt", gmail, addresses).Select(a => a.Id).ToArray());
            Assert.Empty(CaptureIdentity.SimilarAddresses("bob", gmail, addresses));
        }

        [Fact]
        public void SameIdentity_finds_accounts_by_username_or_address()
        {
            var address = new EmailAddress { Id = 10, Username = "fulano", ProviderId = 1 };
            var accounts = new List<Email>
            {
                new() { Id = 1, Username = "nick123", EmailAddressId = 10, PlatformId = 2 },
                new() { Id = 2, Username = "fulano", EmailAddressId = null, PlatformId = 6 },
                new() { Id = 3, Username = "outro", EmailAddressId = 99, PlatformId = 1 },
            };

            var found = CaptureIdentity.SameIdentity("fulano@gmail.com", "fulano", address, accounts);

            Assert.Equal(new[] { 1, 2 }, found.Select(e => e.Id).ToArray());
        }
    }
}
