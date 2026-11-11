using API.Services;

namespace API.DTOs
{
    public class LightingDayDTO
    {
        /// <summary>Local date (yyyy-MM-dd) of the evening. Sunrise and sunset are on this date; LightsOff is the next morning.</summary>
        public DateOnly Date { get; set; }
        public DateTimeOffset? Sunrise { get; set; }
        public DateTimeOffset? Sunset { get; set; }
        public DateTimeOffset? LightsOn { get; set; }
        public DateTimeOffset? DimStart { get; set; }
        public DateTimeOffset? DimEnd { get; set; }
        public DateTimeOffset? LightsOff { get; set; }
        public double LitHours { get; set; }
        public double EstimatedKwh { get; set; }

        public static LightingDayDTO From(LightingDay day) => new()
        {
            Date = day.Date,
            Sunrise = day.Sunrise,
            Sunset = day.Sunset,
            LightsOn = day.LightsOn,
            DimStart = day.DimStart,
            DimEnd = day.DimEnd,
            LightsOff = day.LightsOff,
            LitHours = day.LitHours,
            EstimatedKwh = day.EstimatedKwh,
        };
    }

    public class LightingNowDTO
    {
        /// <summary>On, Dimmed or Off.</summary>
        public string Status { get; set; }
        public int BrightnessPercent { get; set; }
        public LightingNextChangeDTO NextChange { get; set; }
    }

    public class LightingNextChangeDTO
    {
        public DateTimeOffset At { get; set; }
        /// <summary>On, Dimmed or Off.</summary>
        public string ToStatus { get; set; }
    }
}
