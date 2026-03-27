using System;
using Newtonsoft.Json;
using JsonHelpers.Converters;

public static class JsonHelper
{
    public static readonly JsonSerializerSettings SETTINGS = new JsonSerializerSettings()
    {
        Converters =
        {
            new Vector3Converter(),
            new Vector2Converter(),
            new QuaternionConverter(),
            new ColorConverter(),
        }
    };
    
    public static string Serialize(object obj) => JsonConvert.SerializeObject(obj, SETTINGS);
    public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SETTINGS);
    public static object Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, SETTINGS);
}

