using System.Text.Json;

namespace UnityEngine
{
    public static class Application
    {
        public static string persistentDataPath { get; set; } = ".";
    }

    public static class Debug
    {
        public static void LogWarning(object message)
        {
        }
    }

    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
        };

        public static T FromJson<T>(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, ReadOptions);
        }

        public static string ToJson<T>(T value, bool prettyPrint = false)
        {
            return JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = prettyPrint,
                });
        }
    }
}
