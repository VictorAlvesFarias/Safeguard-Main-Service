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

        [Theory]
        [InlineData("Gov.br")]
        [InlineData("GOV.BR")]
        [InlineData("gov br")]
        [InlineData("Gov")]
        public void MatchPlatform_recognizes_the_govbr_login(string platformName)
        {
            var platforms = new List<Platform>(Platforms) { P(7, platformName) };
            Assert.Equal(7, CaptureIdentity.MatchPlatform("sso.acesso.gov.br", "gov.br", platforms)?.Id);
            Assert.Equal(7, CaptureIdentity.MatchPlatform("sso.acesso.gov.br", null, platforms)?.Id);
        }

        [Fact]
        public void A_platform_named_after_the_site_beats_govbr()
        {
            var platforms = new List<Platform>(Platforms) { P(7, "gov.br"), P(8, "Receita") };
            Assert.Equal(8, CaptureIdentity.MatchPlatform("cav.receita.fazenda.gov.br", "Receita", platforms)?.Id);
            Assert.Null(CaptureIdentity.MatchPlatform("sso.acesso.gov.br", null, Platforms));
            Assert.Null(CaptureIdentity.MatchPlatform("www.exemplo.com.br", null, platforms));
        }

        [Theory]
        [InlineData("https://www.Acesso.gov.br/login?x=1", "acesso.gov.br")]
        [InlineData("sso.acesso.gov.br", "sso.acesso.gov.br")]
        [InlineData("  steampowered.com/  ", "steampowered.com")]
        [InlineData("http://localhost:8100", "localhost")]
        public void NormalizeSiteDomain_keeps_the_host(string input, string expected)
        {
            Assert.Equal(expected, CaptureIdentity.NormalizeSiteDomain(input));
        }

        [Theory]
        [InlineData("sso.acesso.gov.br", "acesso.gov.br")]
        [InlineData("accounts.google.com", "google.com")]
        [InlineData("www.itau.com.br", "itau.com.br")]
        [InlineData("store.steampowered.com", "steampowered.com")]
        [InlineData("facebook.com", "facebook.com")]
        [InlineData("127.0.0.1", "127.0.0.1")]
        public void RegistrableDomain_covers_the_site(string host, string expected)
        {
            Assert.Equal(expected, CaptureIdentity.RegistrableDomain(host));
        }

        [Fact]
        public void MatchDomain_covers_subdomains_and_prefers_the_most_specific()
        {
            var domains = new List<PlatformDomain>
            {
                new() { PlatformId = 1, Domain = "gov.br" },
                new() { PlatformId = 2, Domain = "acesso.gov.br" },
                new() { PlatformId = 3, Domain = "steampowered.com" },
            };
            Assert.Equal(2, CaptureIdentity.MatchDomain("sso.acesso.gov.br", domains)?.PlatformId);
            Assert.Equal(1, CaptureIdentity.MatchDomain("www.receita.gov.br", domains)?.PlatformId);
            Assert.Equal(3, CaptureIdentity.MatchDomain("store.steampowered.com", domains)?.PlatformId);
            Assert.Null(CaptureIdentity.MatchDomain("notsteampowered.com", domains));
            Assert.Null(CaptureIdentity.MatchDomain("steampowered.com.evil.io", domains));
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
