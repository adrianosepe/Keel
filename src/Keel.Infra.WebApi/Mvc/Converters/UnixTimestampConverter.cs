using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keel.Infra.WebApi.Mvc.Converters;

public class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var secondsSinceEpoch = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(secondsSinceEpoch).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTime = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unixTime);
    }
}
