using System;
using Newtonsoft.Json;
using UnityEngine;

namespace JsonHelpers.Converters
{
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            var angles = value.eulerAngles;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(angles.x);
            writer.WritePropertyName("y");
            writer.WriteValue(angles.y);
            writer.WritePropertyName("z");
            writer.WriteValue(angles.z);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = reader.Value.ToString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = Convert.ToSingle(reader.Value);
                            break;
                        case "y":
                            y = Convert.ToSingle(reader.Value);
                            break;
                        case "z":
                            z = Convert.ToSingle(reader.Value);
                            break;
                    }
                }
            }

            return Quaternion.Euler(x, y, z);
        }
    }
}