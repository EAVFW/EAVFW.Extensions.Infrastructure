using Newtonsoft.Json;
using System;
using System.Text.Json;

namespace EAVFW.Extensions.Infrastructure.Serialization
{
    public class SystemTextJsonConverter : JsonConverter<JsonElement>
    {

        public override JsonElement ReadJson(JsonReader reader, Type objectType, JsonElement existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, JsonElement value, Newtonsoft.Json.JsonSerializer serializer)
        {

            switch (value.ValueKind)
            {
                case JsonValueKind.Undefined:
                    writer.WriteUndefined();
                    break;
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var prop in value.EnumerateObject())
                    {
                        writer.WritePropertyName(prop.Name);
                        WriteJson(writer, prop.Value, serializer);
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var prop in value.EnumerateArray())
                    {
                        //   writer.WritePropertyName(prop.Name);
                        WriteJson(writer, prop, serializer);
                    }
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.String:
                    writer.WriteValue(value.GetString());
                    break;
                case JsonValueKind.Number:
                    writer.WriteValue(value.GetDecimal());
                    break;
                case JsonValueKind.True:
                    writer.WriteValue(true);
                    break;
                case JsonValueKind.False:
                    writer.WriteValue(false);
                    break;
                case JsonValueKind.Null:
                    writer.WriteNull();
                    break;
                default:
                    break;
            }

            //if(value.ValueKind== JsonValueKind.Object)
            //{

            //    return;
            //}

            //var raw = value.GetRawText();

            //writer.WriteRaw(raw);

        }
    }

}
