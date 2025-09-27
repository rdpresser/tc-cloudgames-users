using System.Text.Json;
using System.Text.Json.Serialization;

namespace TC.CloudGames.Users.Infrastructure.Serialization
{
    public sealed class RoleJsonConverter : JsonConverter<Role>
    {
        public override Role Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Role.FromDb(reader.GetString()!).Value;

        public override void Write(Utf8JsonWriter writer, Role value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Value);
    }
}
