namespace API.Entities
{
    /// <summary>Append-only record of an admin action.</summary>
    public class AuditEntry
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public DateTime AtUtc { get; set; }
        /// <summary>Human-readable summary, at most 500 characters.</summary>
        public string Details { get; set; }
    }
}
