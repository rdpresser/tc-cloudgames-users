using System.Text.Json;
using System.Text.Json.Serialization;

namespace TC.CloudGames.Users.Infrastructure.Serialization
{
    public sealed class PasswordJsonConverter : JsonConverter<Password>
    {
        public override Password Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Password.FromHash(reader.GetString()!).Value;

        public override void Write(Utf8JsonWriter writer, Password value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Hash);
    }
}
