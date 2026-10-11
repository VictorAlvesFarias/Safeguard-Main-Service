using System.Globalization;
using System.Text;
using Domain.Entitites;

namespace Application.Services.CaptureService
{
    public static class CaptureIdentity
    {
        private static readonly HashSet<string> GenericSecondLevel = new() { "com", "net", "org", "gov", "edu", "co", "ac" };

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var decomposed = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString();
        }

        public static string DomainKey(string? domain)
        {
            var labels = (domain ?? "").Trim().ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (labels.Length < 2 || labels.All(l => l.All(char.IsDigit)))
            {
                return string.Join(".", labels);
            }

            var secondLevel = labels.Length > 2 && GenericSecondLevel.Contains(labels[^2]);
            return labels[labels.Length - (secondLevel ? 3 : 2)];
        }

        public static (string? LocalPart, string? Domain) SplitEmail(string? value)
        {
            var text = (value ?? "").Trim().ToLowerInvariant();
            var at = text.IndexOf('@');
            return at > 0 && at < text.Length - 1 ? (text[..at], text[(at + 1)..]) : (null, null);
        }

        public static Platform? MatchPlatform(string? domain, string? platformHint, IEnumerable<Platform> platforms)
        {
            var key = Normalize(DomainKey(domain));
            var hint = Normalize(platformHint);
            Platform? best = null;
            var bestScore = 0;

            foreach (var platform in platforms)
            {
                var name = Normalize(platform.Name);
                if (name.Length == 0)
                {
                    continue;
                }

                var score = name == key || name == hint ? 3
                    : name.Length >= 4 && key.Length >= 4 && (key.Contains(name) || name.Contains(key)) ? 2
                    : 0;

                if (score > bestScore)
                {
                    best = platform;
                    bestScore = score;
                }
            }

            return best;
        }

        public static Provider? MatchProvider(string? emailDomain, IEnumerable<Provider> providers)
        {
            if (string.IsNullOrWhiteSpace(emailDomain))
            {
                return null;
            }

            var list = providers.ToList();
            return list.FirstOrDefault(p => string.Equals(p.Signature?.Trim(), emailDomain, StringComparison.OrdinalIgnoreCase))
                ?? list.FirstOrDefault(p => Normalize(p.Name) == Normalize(DomainKey(emailDomain)));
        }

        public static EmailAddress? MatchAddress(string? localPart, Provider? provider, IEnumerable<EmailAddress> addresses)
        {
            if (string.IsNullOrWhiteSpace(localPart))
            {
                return null;
            }

            var candidates = addresses
                .Where(a => string.Equals(a.Username?.Trim(), localPart, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return provider is null
                ? candidates.FirstOrDefault()
                : candidates.FirstOrDefault(a => a.ProviderId == provider.Id);
        }

        public static int Distance(string a, string b)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();
            var previous = Enumerable.Range(0, b.Length + 1).ToArray();
            for (var i = 1; i <= a.Length; i++)
            {
                var current = new int[b.Length + 1];
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                previous = current;
            }
            return previous[b.Length];
        }

        public static List<EmailAddress> SimilarAddresses(string? localPart, Provider? provider, IEnumerable<EmailAddress> addresses)
        {
            if (string.IsNullOrWhiteSpace(localPart) || localPart.Length < 4)
            {
                return new List<EmailAddress>();
            }

            var maxDistance = localPart.Length <= 6 ? 1 : 2;
            return addresses
                .Where(a => provider is null || a.ProviderId == provider.Id)
                .Where(a => !string.IsNullOrWhiteSpace(a.Username))
                .Select(a => (Address: a, Distance: Distance(localPart, a.Username.Trim())))
                .Where(x => x.Distance > 0 && x.Distance <= maxDistance)
                .OrderBy(x => x.Distance)
                .Take(3)
                .Select(x => x.Address)
                .ToList();
        }

        public static List<Email> SameIdentity(string username, string? localPart, EmailAddress? address, IEnumerable<Email> accounts)
        {
            var normalized = username.Trim().ToLowerInvariant();
            return accounts
                .Where(e =>
                {
                    var accountUsername = (e.Username ?? "").Trim().ToLowerInvariant();
                    return (accountUsername.Length > 0 && (accountUsername == normalized || accountUsername == localPart))
                        || (address is not null && e.EmailAddressId == address.Id);
                })
                .ToList();
        }
    }
}
