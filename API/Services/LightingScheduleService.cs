using API.Entities;

namespace API.Services
{
    /// <summary>Where and how the streetlights run. Built from the municipality settings.</summary>
    public sealed record LightingSite(
        double Latitude,
        double Longitude,
        TimeZoneInfo TimeZone,
        int StreetlightCount,
        int StreetlightWatts);

    /// <summary>
    /// One night, keyed by its evening date. Times are UTC, and null when the sun does not set or rise that day.
    /// Lights come on at sunset − 10 min, dim to 50% from 00:30 to 05:00 local the next morning, and go off at
    /// sunrise + 10 min the following morning.
    /// </summary>
    public sealed record LightingNight(
        DateOnly Date,
        DateTime? SunriseUtc,
        DateTime? SunsetUtc,
        DateTime? OnUtc,
        DateTime? DimStartUtc,
        DateTime? DimEndUtc,
        DateTime? OffUtc);

    /// <summary>A schedule row. Sunrise and sunset belong to Date; LightsOff is the next morning.</summary>
    public sealed record LightingDay(
        DateOnly Date,
        DateTimeOffset? Sunrise,
        DateTimeOffset? Sunset,
        DateTimeOffset? LightsOn,
        DateTimeOffset? DimStart,
        DateTimeOffset? DimEnd,
        DateTimeOffset? LightsOff,
        double LitHours,
        double EstimatedKwh);

    public sealed record LightingChange(DateTime AtUtc, string ToStatus);

    public sealed record LightingNow(string Status, int BrightnessPercent, LightingChange NextChange);

    /// <summary>
    /// The fixed lighting policy on top of NOAA sunrise and sunset. Deterministic: no external calls.
    /// </summary>
    public static class LightingScheduleService
    {
        public const string On = "On";
        public const string Dimmed = "Dimmed";
        public const string Off = "Off";

        public const int FullBrightness = 100;
        public const int DimBrightness = 50;

        public static readonly TimeSpan LightsOnBeforeSunset = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan LightsOffAfterSunrise = TimeSpan.FromMinutes(10);
        public static readonly TimeOnly DimStartLocal = new(0, 30);
        public static readonly TimeOnly DimEndLocal = new(5, 0);

        public static LightingSite SiteFor(Municipality municipality) => new(
            municipality.CenterLatitude,
            municipality.CenterLongitude,
            TimeZoneInfo.FindSystemTimeZoneById(municipality.TimeZoneId),
            municipality.StreetlightCount,
            municipality.StreetlightWatts);

        /// <summary>The local calendar date at the given UTC instant.</summary>
        public static DateOnly LocalDate(LightingSite site, DateTime utc) =>
            DateOnly.FromDateTime(ToLocal(site, utc).DateTime);

        public static LightingNight GetNight(LightingSite site, DateOnly date)
        {
            var tonight = SolarCalculator.Calculate(date, site.Latitude, site.Longitude);
            var tomorrow = SolarCalculator.Calculate(date.AddDays(1), site.Latitude, site.Longitude);

            var onUtc = tonight.Sunset - LightsOnBeforeSunset;
            var offUtc = tomorrow.Sunrise + LightsOffAfterSunrise;
            if (onUtc == null || offUtc == null)
            {
                return new LightingNight(date, tonight.Sunrise, tonight.Sunset, null, null, null, null);
            }

            var nextMorning = date.AddDays(1);
            return new LightingNight(
                date,
                tonight.Sunrise,
                tonight.Sunset,
                onUtc,
                LocalToUtc(site, nextMorning, DimStartLocal),
                LocalToUtc(site, nextMorning, DimEndLocal),
                offUtc);
        }

        public static LightingDay GetDay(LightingSite site, DateOnly date)
        {
            var night = GetNight(site, date);
            var sunrise = ToLocalOrNull(site, night.SunriseUtc);
            var sunset = ToLocalOrNull(site, night.SunsetUtc);

            if (night.OnUtc == null || night.OffUtc == null)
            {
                return new LightingDay(date, sunrise, sunset, null, null, null, null, 0, 0);
            }

            var on = night.OnUtc.Value;
            var dimStart = night.DimStartUtc.Value;
            var dimEnd = night.DimEndUtc.Value;
            var off = night.OffUtc.Value;

            var litHours = (off - on).TotalHours;
            // Σ(hours × level), with level as a fraction of full brightness.
            var weightedHours =
                Hours(on, dimStart) * (FullBrightness / 100.0) +
                Hours(dimStart, dimEnd) * (DimBrightness / 100.0) +
                Hours(dimEnd, off) * (FullBrightness / 100.0);
            var kwh = site.StreetlightCount * (double)site.StreetlightWatts * weightedHours / 1000.0;

            return new LightingDay(
                date,
                sunrise,
                sunset,
                ToLocalOrNull(site, on),
                ToLocalOrNull(site, dimStart),
                ToLocalOrNull(site, dimEnd),
                ToLocalOrNull(site, off),
                Math.Round(litHours, 2),
                Math.Round(kwh, 2));
        }

        public static List<LightingDay> GetSchedule(LightingSite site, DateOnly from, int days)
        {
            var rows = new List<LightingDay>(days);
            for (var i = 0; i < days; i++) rows.Add(GetDay(site, from.AddDays(i)));
            return rows;
        }

        /// <summary>
        /// The current status at a UTC instant, from the events of the nights around it:
        /// On at 100%, Dimmed at 50% and Off at 0%.
        /// </summary>
        public static LightingNow GetNow(LightingSite site, DateTime utcNow)
        {
            var today = LocalDate(site, utcNow);
            var events = new List<(DateTime At, string Status)>();
            for (var offset = -2; offset <= 2; offset++)
            {
                var night = GetNight(site, today.AddDays(offset));
                if (night.OnUtc == null || night.OffUtc == null) continue;

                events.Add((night.OnUtc.Value, On));
                events.Add((night.DimStartUtc.Value, Dimmed));
                events.Add((night.DimEndUtc.Value, On));
                events.Add((night.OffUtc.Value, Off));
            }
            events.Sort((a, b) => a.At.CompareTo(b.At));

            var status = Off;
            foreach (var evt in events)
            {
                if (evt.At <= utcNow) status = evt.Status;
            }

            var brightness = status switch
            {
                On => FullBrightness,
                Dimmed => DimBrightness,
                _ => 0,
            };

            // The list is sorted, so the first event after now is the next change.
            var next = events
                .Where(e => e.At > utcNow)
                .Select(e => new LightingChange(e.At, e.Status))
                .FirstOrDefault();

            return new LightingNow(status, brightness, next);
        }

        private static double Hours(DateTime from, DateTime to) => Math.Max(0, (to - from).TotalHours);

        private static DateTime LocalToUtc(LightingSite site, DateOnly date, TimeOnly time) =>
            TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time), site.TimeZone);

        private static DateTimeOffset ToLocal(LightingSite site, DateTime utc) =>
            TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)), site.TimeZone);

        private static DateTimeOffset? ToLocalOrNull(LightingSite site, DateTime? utc) =>
            utc.HasValue ? ToLocal(site, utc.Value) : null;
    }
}
