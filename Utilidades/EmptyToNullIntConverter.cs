using System.Text.Json;
using System.Text.Json.Serialization;

namespace RequisicionesApi.Utilidades
{
    public class EmptyToNullIntConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
                return null;

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value))
                return value;

            return null;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
