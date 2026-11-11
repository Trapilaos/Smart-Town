using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace API.Services
{
    /// <summary>Settings for <see cref="LoginThrottle"/>. Bound from the LoginThrottle configuration section.</summary>
    public sealed class LoginThrottleOptions
    {
        /// <summary>Failures for one username and address that start a block.</summary>
        public int MaxFailures { get; set; } = 5;

        /// <summary>Failures count towards the limit only within this window, measured from the first failure.</summary>
        public int WindowSeconds { get; set; } = 600;

        /// <summary>How long one username and address stays blocked once it reaches the limit.</summary>
        public int BlockSeconds { get; set; } = 600;
    }

    /// <summary>
    /// Throttles failed logins per (username, client address). One attacker can lock out only the pairs they
    /// reach, never the account itself: the real user, from another address, keeps working. A successful login
    /// clears the pair. State lives in a memory cache, so it is per process and expires on its own.
    /// </summary>
    public sealed class LoginThrottle
    {
        private readonly IMemoryCache _cache;
        private readonly LoginThrottleOptions _options;
        private readonly object _gate = new();

        private sealed class Attempts
        {
            public int Failures { get; set; }
            public DateTimeOffset WindowStart { get; set; }
            public DateTimeOffset? BlockedUntil { get; set; }
        }

        public LoginThrottle(IMemoryCache cache, IOptions<LoginThrottleOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public bool IsBlocked(string username, string clientAddress)
        {
            var key = Key(username, clientAddress);
            lock (_gate)
            {
                return _cache.TryGetValue(key, out Attempts attempts)
                    && attempts.BlockedUntil is { } until
                    && until > DateTimeOffset.UtcNow;
            }
        }

        public void RecordFailure(string username, string clientAddress)
        {
            var key = Key(username, clientAddress);
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromSeconds(_options.WindowSeconds);

            lock (_gate)
            {
                var attempts = _cache.TryGetValue(key, out Attempts existing) ? existing : null;

                if (attempts == null
                    || now - attempts.WindowStart > window
                    || attempts.BlockedUntil is { } expired && expired <= now)
                {
                    attempts = new Attempts { WindowStart = now };
                }
                else if (attempts.BlockedUntil is { } blockedUntil && blockedUntil > now)
                {
                    return; // already blocked: nothing more to count
                }

                attempts.Failures++;
                if (attempts.Failures >= _options.MaxFailures)
                {
                    attempts.BlockedUntil = now.AddSeconds(_options.BlockSeconds);
                    attempts.Failures = 0;
                }

                _cache.Set(key, attempts, window + TimeSpan.FromSeconds(_options.BlockSeconds));
            }
        }

        public void Reset(string username, string clientAddress)
        {
            lock (_gate)
            {
                _cache.Remove(Key(username, clientAddress));
            }
        }

        private static string Key(string username, string clientAddress) =>
            $"login-throttle|{username}|{clientAddress}";
    }
}
