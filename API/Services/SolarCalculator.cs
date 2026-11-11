namespace API.Services
{
    /// <summary>Sunrise, sunset and civil twilight in UTC. A null value means the sun does not cross that threshold that day.</summary>
    public sealed record SolarEvents(DateTime? Sunrise, DateTime? Sunset, DateTime? CivilDawn, DateTime? CivilDusk);

    /// <summary>
    /// NOAA solar position algorithm (Solar Calculator spreadsheet formulas), pure and static.
    /// Accurate to about a minute for latitudes below the polar circles.
    /// </summary>
    public static class SolarCalculator
    {
        /// <summary>Sun centre at -0.833 degrees: the standard sunrise and sunset, including refraction.</summary>
        public const double OfficialZenith = 90.833;

        /// <summary>Sun centre at -6 degrees: civil dawn and dusk.</summary>
        public const double CivilZenith = 96.0;

        /// <summary>Sunrise, sunset and civil twilight for a UTC calendar date at the given location (east longitude positive).</summary>
        public static SolarEvents Calculate(DateOnly date, double latitude, double longitude)
        {
            var midnightUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var julianDayAtMidnight = JulianDay(midnightUtc);

            DateTime? At(double? minutes) => minutes.HasValue ? midnightUtc.AddSeconds(Math.Round(minutes.Value * 60)) : null;

            return new SolarEvents(
                Sunrise: At(EventMinutes(julianDayAtMidnight, latitude, longitude, OfficialZenith, rising: true)),
                Sunset: At(EventMinutes(julianDayAtMidnight, latitude, longitude, OfficialZenith, rising: false)),
                CivilDawn: At(EventMinutes(julianDayAtMidnight, latitude, longitude, CivilZenith, rising: true)),
                CivilDusk: At(EventMinutes(julianDayAtMidnight, latitude, longitude, CivilZenith, rising: false)));
        }

        /// <summary>Minutes after 00:00 UTC of the date, or null when the sun never reaches the zenith angle.</summary>
        private static double? EventMinutes(double julianDayAtMidnight, double latitude, double longitude, double zenith, bool rising)
        {
            // Start from solar noon, then refine: the declination and equation of time depend on the time of day.
            var minutes = 720.0 - 4.0 * longitude;
            for (var iteration = 0; iteration < 4; iteration++)
            {
                var century = JulianCentury(julianDayAtMidnight + minutes / 1440.0);
                var (declination, equationOfTime) = SunDeclinationAndEquationOfTime(century);

                var solarNoon = 720.0 - 4.0 * longitude - equationOfTime;
                var latRad = ToRadians(latitude);
                var decRad = ToRadians(declination);

                var cosHourAngle = Math.Cos(ToRadians(zenith)) / (Math.Cos(latRad) * Math.Cos(decRad))
                                   - Math.Tan(latRad) * Math.Tan(decRad);
                if (cosHourAngle < -1 || cosHourAngle > 1) return null;

                var hourAngle = ToDegrees(Math.Acos(cosHourAngle));
                minutes = rising ? solarNoon - 4.0 * hourAngle : solarNoon + 4.0 * hourAngle;
            }
            return minutes;
        }

        /// <summary>Solar declination (degrees) and equation of time (minutes) for a Julian century.</summary>
        private static (double Declination, double EquationOfTime) SunDeclinationAndEquationOfTime(double t)
        {
            var meanLongitude = Mod360(280.46646 + t * (36000.76983 + t * 0.0003032));
            var meanAnomaly = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            var eccentricity = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

            var m = ToRadians(meanAnomaly);
            var centre = Math.Sin(m) * (1.914602 - t * (0.004817 + 0.000014 * t))
                       + Math.Sin(2 * m) * (0.019993 - 0.000101 * t)
                       + Math.Sin(3 * m) * 0.000289;

            var trueLongitude = meanLongitude + centre;
            var omega = ToRadians(125.04 - 1934.136 * t);
            var apparentLongitude = trueLongitude - 0.00569 - 0.00478 * Math.Sin(omega);

            var meanObliquity = 23 + (26 + (21.448 - t * (46.815 + t * (0.00059 - t * 0.001813))) / 60) / 60;
            var obliquity = meanObliquity + 0.00256 * Math.Cos(omega);

            var declination = ToDegrees(Math.Asin(Math.Sin(ToRadians(obliquity)) * Math.Sin(ToRadians(apparentLongitude))));

            var y = Math.Tan(ToRadians(obliquity) / 2);
            y *= y;
            var l0 = ToRadians(meanLongitude);
            var equationOfTime = 4 * ToDegrees(
                y * Math.Sin(2 * l0)
                - 2 * eccentricity * Math.Sin(m)
                + 4 * eccentricity * y * Math.Sin(m) * Math.Cos(2 * l0)
                - 0.5 * y * y * Math.Sin(4 * l0)
                - 1.25 * eccentricity * eccentricity * Math.Sin(2 * m));

            return (declination, equationOfTime);
        }

        private static double JulianDay(DateTime utc) =>
            (utc - DateTime.UnixEpoch).TotalDays + 2440587.5;

        private static double JulianCentury(double julianDay) => (julianDay - 2451545.0) / 36525.0;

        private static double Mod360(double degrees) => degrees - 360.0 * Math.Floor(degrees / 360.0);

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
    }
}
