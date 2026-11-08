using System.Security.Cryptography;

namespace Application.Services.AuthenticatorService
{
    /// <summary>Time-based one-time codes (RFC 6238), same as the app's authenticator page.</summary>
    public static class Totp
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static byte[] DecodeBase32(string secret)
        {
            var clean = secret.Replace(" ", "").Replace("-", "").TrimEnd('=').ToUpperInvariant();
            var bytes = new List<byte>(clean.Length * 5 / 8);
            int buffer = 0, bits = 0;

            foreach (var c in clean)
            {
                var value = Base32Alphabet.IndexOf(c);
                if (value < 0)
                {
                    throw new FormatException("Chave secreta inválida.");
                }

                buffer = (buffer << 5) | value;
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    bytes.Add((byte)((buffer >> bits) & 0xFF));
                }
            }

            return bytes.ToArray();
        }

        /// <summary>The code for <paramref name="unixSeconds"/> and the seconds it stays valid.</summary>
        public static (string Code, int RemainingSeconds) Generate(string secret, string algorithm, int digits, int period, long unixSeconds)
        {
            var key = DecodeBase32(secret);
            var counter = unixSeconds / period;
            var message = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(message);
            }

            using HMAC hmac = (algorithm ?? "SHA1").ToUpperInvariant() switch
            {
                "SHA256" => new HMACSHA256(key),
                "SHA512" => new HMACSHA512(key),
                _ => new HMACSHA1(key),
            };

            var hash = hmac.ComputeHash(message);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            var code = (binary % (int)Math.Pow(10, digits)).ToString().PadLeft(digits, '0');

            return (code, period - (int)(unixSeconds % period));
        }
    }
}
