namespace API.Extensions
{
    /// <summary>
    /// Central place for reading JWT-related configuration.
    /// The signing key must come from user-secrets or the TokenKey environment variable, never from source control.
    /// </summary>
    public static class JwtSettings
    {
        public const int MinimumKeyLength = 64; // HS512 needs a key of at least 512 bits

        public static string GetTokenKey(IConfiguration config)
        {
            var key = config["TokenKey"];

            if (string.IsNullOrWhiteSpace(key) || key.Length < MinimumKeyLength || key.StartsWith("CHANGE-ME"))
            {
                throw new InvalidOperationException(
                    $"Configuration value 'TokenKey' is missing, still the placeholder, or shorter than {MinimumKeyLength} characters. " +
                    "Set it with: dotnet user-secrets set \"TokenKey\" \"<random string of 64+ characters>\" " +
                    "(or the TokenKey environment variable).");
            }

            return key;
        }

        public static string GetIssuer(IConfiguration config) => config["Jwt:Issuer"] ?? "SmartTown.Api";

        public static string GetAudience(IConfiguration config) => config["Jwt:Audience"] ?? "SmartTown.Client";

        public static TimeSpan GetLifetime(IConfiguration config) =>
            TimeSpan.FromDays(config.GetValue("Jwt:ExpiryDays", 7));
    }
}
