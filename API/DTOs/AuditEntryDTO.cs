namespace API.DTOs
{
    /// <summary>One admin action. The actor's username is shown; ids are not.</summary>
    public class AuditEntryDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public DateTime AtUtc { get; set; }
        public string Details { get; set; }
    }
}
