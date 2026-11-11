using API.Services;

namespace API.Tests;

public class SolarAndLightingTests
{
    private const double AthensLat = 37.98;
    private const double AthensLng = 23.73;

    private static readonly TimeZoneInfo Athens = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

    private static readonly LightingSite Site = new(37.8650, 23.7530, Athens, 1200, 60);

    private static TimeSpan LocalTimeOfDay(DateTime utc) =>
        TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)), Athens).TimeOfDay;

    private static void AssertTimeOfDay(DateTime? utc, TimeSpan expected, int toleranceMinutes)
    {
        Assert.NotNull(utc);
        var actual = LocalTimeOfDay(utc.Value);
        Assert.True(
            Math.Abs((actual - expected).TotalMinutes) <= toleranceMinutes,
            $"expected about {expected}, got {actual}");
    }

    [Fact]
    public void Athens_midsummer_sunrise_and_sunset_match_noaa()
    {
        var events = SolarCalculator.Calculate(new DateOnly(2026, 6, 21), AthensLat, AthensLng);

        AssertTimeOfDay(events.Sunrise, new TimeSpan(6, 2, 0), 5);
        AssertTimeOfDay(events.Sunset, new TimeSpan(20, 51, 0), 5);
    }

    [Fact]
    public void Athens_midwinter_sunrise_and_sunset_match_noaa()
    {
        var events = SolarCalculator.Calculate(new DateOnly(2026, 12, 21), AthensLat, AthensLng);

        AssertTimeOfDay(events.Sunrise, new TimeSpan(7, 38, 0), 5);
        AssertTimeOfDay(events.Sunset, new TimeSpan(17, 9, 0), 5);
    }

    [Fact]
    public void Civil_twilight_brackets_sunrise_and_sunset()
    {
        var events = SolarCalculator.Calculate(new DateOnly(2026, 6, 21), AthensLat, AthensLng);

        Assert.True(events.CivilDawn < events.Sunrise);
        Assert.True(events.Sunset < events.CivilDusk);
    }

    [Fact]
    public void Midnight_sun_has_no_sunset()
    {
        var events = SolarCalculator.Calculate(new DateOnly(2026, 6, 21), 80, 15);

        Assert.Null(events.Sunrise);
        Assert.Null(events.Sunset);
    }

    [Fact]
    public void Every_night_of_2026_is_ordered_on_dim_dimEnd_off()
    {
        var date = new DateOnly(2026, 1, 1);
        for (var i = 0; i < 365; i++, date = date.AddDays(1))
        {
            var night = LightingScheduleService.GetNight(Site, date);

            Assert.NotNull(night.OnUtc);
            Assert.True(night.OnUtc < night.DimStartUtc, $"{date}: lights on must precede the dim");
            Assert.True(night.DimStartUtc < night.DimEndUtc, $"{date}: dim must start before it ends");
            Assert.True(night.DimEndUtc < night.OffUtc, $"{date}: dim must end before lights go off");
        }
    }

    [Fact]
    public void Dim_window_is_00_30_to_05_00_local_on_the_following_morning()
    {
        var night = LightingScheduleService.GetNight(Site, new DateOnly(2026, 6, 21));

        Assert.Equal(new TimeSpan(0, 30, 0), LocalTimeOfDay(night.DimStartUtc.Value));
        Assert.Equal(new TimeSpan(5, 0, 0), LocalTimeOfDay(night.DimEndUtc.Value));
        Assert.Equal(new DateTime(2026, 6, 22, 0, 30, 0),
            TimeZoneInfo.ConvertTimeFromUtc(night.DimStartUtc.Value, Athens));
    }

    [Fact]
    public void Lights_come_on_ten_minutes_before_sunset_and_go_off_ten_minutes_after_sunrise()
    {
        var date = new DateOnly(2026, 6, 21);
        var night = LightingScheduleService.GetNight(Site, date);
        var sunset = SolarCalculator.Calculate(date, Site.Latitude, Site.Longitude).Sunset;
        var nextSunrise = SolarCalculator.Calculate(date.AddDays(1), Site.Latitude, Site.Longitude).Sunrise;

        Assert.Equal(sunset - TimeSpan.FromMinutes(10), night.OnUtc);
        Assert.Equal(nextSunrise + TimeSpan.FromMinutes(10), night.OffUtc);
    }

    [Fact]
    public void Energy_matches_the_formula_count_times_watts_times_weighted_hours()
    {
        var date = new DateOnly(2026, 12, 21);
        var night = LightingScheduleService.GetNight(Site, date);
        var day = LightingScheduleService.GetDay(Site, date);

        // Dimmed hours count at half weight: weighted = lit - 0.5 * dim (unrounded, from the night's own times).
        var litHours = (night.OffUtc.Value - night.OnUtc.Value).TotalHours;
        var dimHours = (night.DimEndUtc.Value - night.DimStartUtc.Value).TotalHours;
        var expected = 1200 * 60 * (litHours - 0.5 * dimHours) / 1000.0;

        Assert.Equal(expected, day.EstimatedKwh, precision: 1);
        Assert.True(day.EstimatedKwh > 0);
    }

    [Fact]
    public void Schedule_returns_consecutive_local_dates()
    {
        var rows = LightingScheduleService.GetSchedule(Site, new DateOnly(2026, 6, 21), 7);

        Assert.Equal(7, rows.Count);
        Assert.Equal(new DateOnly(2026, 6, 21), rows[0].Date);
        Assert.Equal(new DateOnly(2026, 6, 27), rows[6].Date);
    }

    [Fact]
    public void Status_at_23_00_local_in_summer_is_on_at_full_brightness()
    {
        var now = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 6, 21, 23, 0, 0), Athens);

        var status = LightingScheduleService.GetNow(Site, now);

        Assert.Equal(LightingScheduleService.On, status.Status);
        Assert.Equal(100, status.BrightnessPercent);
        Assert.Equal(LightingScheduleService.Dimmed, status.NextChange.ToStatus);
        Assert.Equal(new DateTime(2026, 6, 22, 0, 30, 0), TimeZoneInfo.ConvertTimeFromUtc(status.NextChange.AtUtc, Athens));
    }

    [Fact]
    public void Status_at_02_00_local_is_dimmed_to_half_brightness()
    {
        var now = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 6, 22, 2, 0, 0), Athens);

        var status = LightingScheduleService.GetNow(Site, now);

        Assert.Equal(LightingScheduleService.Dimmed, status.Status);
        Assert.Equal(50, status.BrightnessPercent);
        Assert.Equal(LightingScheduleService.On, status.NextChange.ToStatus);
        Assert.Equal(new DateTime(2026, 6, 22, 5, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(status.NextChange.AtUtc, Athens));
    }

    [Fact]
    public void Status_at_noon_is_off_and_the_next_change_is_the_evening_switch_on()
    {
        var now = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 6, 22, 12, 0, 0), Athens);

        var status = LightingScheduleService.GetNow(Site, now);

        Assert.Equal(LightingScheduleService.Off, status.Status);
        Assert.Equal(0, status.BrightnessPercent);
        Assert.Equal(LightingScheduleService.On, status.NextChange.ToStatus);
        Assert.True(status.NextChange.AtUtc > now);
    }
}
