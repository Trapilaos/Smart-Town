using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Extensions;

namespace API.Helpers
{
    /// <summary>
    /// Writes every DateTime as UTC with a trailing Z. SQLite reads come back with Kind = Unspecified and are
    /// treated as UTC, which is how they were stored. Only DateTime is affected; DateTimeOffset (lighting) is not.
    /// </summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDateTime().ToUtc();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToUtc().ToString("O", CultureInfo.InvariantCulture));
    }
}
