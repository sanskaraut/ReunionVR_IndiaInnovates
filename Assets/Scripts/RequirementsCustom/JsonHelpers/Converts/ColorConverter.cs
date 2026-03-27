using System;
using Newtonsoft.Json;
using UnityEngine;

namespace JsonHelpers.Converters
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue(ColorUtility.ToHtmlStringRGB(value));
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string hex = (string)reader.Value;
                ColorUtility.TryParseHtmlString("#" + hex, out Color color);
                return color;
            }

            return existingValue;
        }
    }
}