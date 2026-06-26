using System.Text.Json;
using System.Text.Json.Serialization;
using ValueOf;

namespace EisenFeed.Core.Models;

[JsonConverter(typeof(FeedItemIdJsonConverter))]
public sealed class FeedItemId : ValueOf<string, FeedItemId>
{
    protected override void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Value);
    }

}

public sealed class FeedItemIdJsonConverter : JsonConverter<FeedItemId>
{
    public override FeedItemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (value is null)
        {
            throw new JsonException("FeedItemId cannot be null.");
        }

        return FeedItemId.From(value);
    }

    public override void Write(Utf8JsonWriter writer, FeedItemId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}