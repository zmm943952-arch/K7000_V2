using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReadConfigLib
{
    public static class JsonConfigReader
    {
        public static string[] GetAllKeys(string jsonPath)
        {
            var dict = LoadAndFlatten(jsonPath);
            return dict.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static string[] GetAllValues(string jsonPath)
        {
            var dict = LoadAndFlatten(jsonPath);
            return dict.Values.ToArray();
        }

        // Returns "key=value" lines for easy parsing in TestStand.
        public static string[] GetAllItems(string jsonPath)
        {
            var dict = LoadAndFlatten(jsonPath);
            return dict.Select(kv => $"{kv.Key}={kv.Value}").ToArray();
        }

        // Returns a JSON array of {"key":"...","value":"..."}
        public static string GetAllItemsAsJson(string jsonPath)
        {
            var dict = LoadAndFlatten(jsonPath);
            var items = dict.Select(kv => new Item { Key = kv.Key, Value = kv.Value }).ToArray();
            return JsonConvert.SerializeObject(items, Formatting.Indented);
        }

        // Get value by flattened key (dot / [index]) or JSONPath.
        public static string GetValue(string jsonPath, string key, string defaultValue = "")
        {
            var token = LoadToken(jsonPath);

            var dict = FlattenToDictionary(token);
            if (dict.TryGetValue(key, out var value))
                return value;

            var selected = token.SelectToken(key, errorWhenNoMatch: false);
            if (selected == null)
                return defaultValue;

            return TokenToString(selected);
        }

        private static Dictionary<string, string> LoadAndFlatten(string jsonPath)
        {
            var token = LoadToken(jsonPath);
            return FlattenToDictionary(token);
        }

        private static JToken LoadToken(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ArgumentException("jsonPath is required.", nameof(jsonPath));

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"JSON file not found: {jsonPath}");

            var json = File.ReadAllText(jsonPath);
            return JToken.Parse(json);
        }

        private static Dictionary<string, string> FlattenToDictionary(JToken token)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Flatten(token, "", dict);
            return dict;
        }

        private static void Flatten(JToken token, string prefix, IDictionary<string, string> dict)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        var next = string.IsNullOrEmpty(prefix) ? prop.Name : prefix + "." + prop.Name;
                        Flatten(prop.Value, next, dict);
                    }
                    break;

                case JTokenType.Array:
                    var index = 0;
                    foreach (var item in (JArray)token)
                    {
                        var next = prefix + "[" + index + "]";
                        Flatten(item, next, dict);
                        index++;
                    }
                    if (!((JArray)token).Any())
                        dict[prefix] = "[]";
                    break;

                default:
                    dict[prefix] = TokenToString(token);
                    break;
            }
        }

        private static string TokenToString(JToken token)
        {
            if (token is JValue value)
            {
                return value.Value?.ToString() ?? string.Empty;
            }

            // For objects/arrays selected via JSONPath, return compact JSON.
            return token.ToString(Formatting.None);
        }

        private sealed class Item
        {
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
        }
    }
}
