using System;
using Newtonsoft.Json.Linq;

namespace RfpTestStation.Adapters.Config
{
    public sealed class StationConfig
    {
        public StationConfig(JObject root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public JObject Root { get; }

        public string GetValue(string path, string defaultValue = "")
        {
            var token = Root.SelectToken(path, errorWhenNoMatch: false);
            return token == null ? defaultValue : token.ToString();
        }

        public void SetValue(string path, string value)
        {
            SetToken(path, value);
        }

        public void SetValue(string path, bool value)
        {
            SetToken(path, value);
        }

        public void SetValue(string path, int value)
        {
            SetToken(path, value);
        }

        public void SetValue(string path, double value)
        {
            SetToken(path, value);
        }

        private void SetToken(string path, object value)
        {
            var parts = path.Split('.');
            if (parts.Length == 0)
            {
                return;
            }

            var current = Root;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                current = GetOrCreateObject(current, parts[i]);
            }

            SetTokenValue(current, parts[parts.Length - 1], JToken.FromObject(value));
        }

        private static JObject GetOrCreateObject(JObject parent, string segment)
        {
            var arraySegment = TryParseArraySegment(segment, out var name, out var index);
            if (!arraySegment)
            {
                var child = parent[segment] as JObject;
                if (child == null)
                {
                    child = new JObject();
                    parent[segment] = child;
                }

                return child;
            }

            var array = parent[name] as JArray;
            if (array == null)
            {
                array = new JArray();
                parent[name] = array;
            }

            while (array.Count <= index)
            {
                array.Add(new JObject());
            }

            var item = array[index] as JObject;
            if (item == null)
            {
                item = new JObject();
                array[index] = item;
            }

            return item;
        }

        private static void SetTokenValue(JObject parent, string segment, JToken value)
        {
            var arraySegment = TryParseArraySegment(segment, out var name, out var index);
            if (!arraySegment)
            {
                parent[segment] = value;
                return;
            }

            var array = parent[name] as JArray;
            if (array == null)
            {
                array = new JArray();
                parent[name] = array;
            }

            while (array.Count <= index)
            {
                array.Add(JValue.CreateNull());
            }

            array[index] = value;
        }

        private static bool TryParseArraySegment(string segment, out string name, out int index)
        {
            name = segment;
            index = 0;

            var open = segment.IndexOf("[", StringComparison.Ordinal);
            var close = segment.IndexOf("]", StringComparison.Ordinal);
            if (open <= 0 || close <= open + 1 || close != segment.Length - 1)
            {
                return false;
            }

            name = segment.Substring(0, open);
            return int.TryParse(segment.Substring(open + 1, close - open - 1), out index);
        }
    }
}
