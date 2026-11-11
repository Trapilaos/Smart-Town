namespace API.Middleware
{
    /// <summary>
    /// Adds defensive headers to every API response. The API only returns JSON, so the CSP blocks all content.
    /// HSTS is sent only outside Development.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        public const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        public const string PermissionsPolicy = "geolocation=(), camera=(), microphone=()";
        public const string StrictTransportSecurity = "max-age=31536000; includeSubDomains";

        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _env;

        public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            headers["Permissions-Policy"] = PermissionsPolicy;

            if (!_env.IsDevelopment())
                headers["Strict-Transport-Security"] = StrictTransportSecurity;

            return _next(context);
        }
    }
}
