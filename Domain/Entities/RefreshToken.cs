namespace Domain.Entitites
{
    /// <summary>
    /// A long-lived login, traded at /refresh-token for a new access token and a new refresh
    /// token (rotation). Only the SHA-256 of the token is stored.
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        /// <summary>Set when rotated; null when revoked by sign-out.</summary>
        public string? ReplacedByHash { get; set; }
    }
}
