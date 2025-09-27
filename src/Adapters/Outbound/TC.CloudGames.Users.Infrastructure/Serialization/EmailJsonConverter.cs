using System.Text.Json;
using System.Text.Json.Serialization;

namespace TC.CloudGames.Users.Infrastructure.Serialization
{
    public sealed class EmailJsonConverter : JsonConverter<Email>
    {
        public override Email Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Email.FromDb(reader.GetString()!).Value;

        public override void Write(Utf8JsonWriter writer, Email value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
