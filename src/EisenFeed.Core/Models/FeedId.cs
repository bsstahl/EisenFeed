using System.Text.Json;
using System.Text.Json.Serialization;
using ValueOf;

namespace EisenFeed.Core.Models;

[JsonConverter(typeof(FeedIdJsonConverter))]
public sealed class FeedId : ValueOf<string, FeedId>
{
    protected override void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Value);
    }

}

public sealed class FeedIdJsonConverter : JsonConverter<FeedId>
{
    public override FeedId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (value is null)
        {
            throw new JsonException("FeedId cannot be null.");
        }

        return FeedId.From(value);
    }

    public override void Write(Utf8JsonWriter writer, FeedId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}