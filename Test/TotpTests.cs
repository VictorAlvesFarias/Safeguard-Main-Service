using Application.Services.AuthenticatorService;
using Xunit;

namespace Test
{
    /// <summary>RFC 6238 appendix B test vectors (8 digits, 30 s).</summary>
    public class TotpTests
    {
        // ASCII "12345678901234567890" (20 bytes), "...012" (32 bytes) and "...01234" (64 bytes) in base32.
        private const string Sha1Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        private const string Sha256Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA";
        private const string Sha512Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA";

        [Theory]
        [InlineData(59L, "94287082", "46119246", "90693936")]
        [InlineData(1111111109L, "07081804", "68084774", "25091201")]
        [InlineData(1111111111L, "14050471", "67062674", "99943326")]
        [InlineData(1234567890L, "89005924", "91819424", "93441116")]
        [InlineData(2000000000L, "69279037", "90698825", "38618901")]
        [InlineData(20000000000L, "65353130", "77737706", "47863826")]
        public void Matches_rfc6238_vectors(long time, string sha1, string sha256, string sha512)
        {
            Assert.Equal(sha1, Totp.Generate(Sha1Secret, "SHA1", 8, 30, time).Code);
            Assert.Equal(sha256, Totp.Generate(Sha256Secret, "SHA256", 8, 30, time).Code);
            Assert.Equal(sha512, Totp.Generate(Sha512Secret, "SHA512", 8, 30, time).Code);
        }

        /// <summary>Same codes as the app's authenticator page (otpauth library) at the same instant.</summary>
        [Theory]
        [InlineData("JBSWY3DPEHPK3PXP", "SHA1", 6, 30, "324550")]
        [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA", "SHA256", 8, 60, "31230549")]
        [InlineData("JBSWY3DPEHPK3PXPJBSWY3DP", "SHA512", 7, 30, "5462292")]
        public void Matches_the_app(string secret, string algorithm, int digits, int period, string expected)
        {
            Assert.Equal(expected, Totp.Generate(secret, algorithm, digits, period, 1700000000).Code);
        }

        [Fact]
        public void Six_digits_and_remaining_seconds()
        {
            var (code, remaining) = Totp.Generate(Sha1Secret, "SHA1", 6, 30, 59);
            Assert.Equal("287082", code);
            Assert.Equal(1, remaining);
        }
    }
}
