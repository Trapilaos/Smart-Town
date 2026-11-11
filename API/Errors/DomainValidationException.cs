namespace API.Errors
{
    /// <summary>A business rule was broken (for example a bin outside the boundary). Controllers map it to 400.</summary>
    public sealed class DomainValidationException : Exception
    {
        public DomainValidationException(string message) : base(message) { }
    }
}
