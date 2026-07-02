using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RfpTestStation.Adapters.Config
{
    public sealed class ConfigRepository
    {
        private readonly string _path;

        public ConfigRepository(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Config path is required.", nameof(path));
            }

            _path = path;
        }

        public StationConfig Load()
        {
            if (!File.Exists(_path))
            {
                throw new FileNotFoundException("Config file not found.", _path);
            }

            return new StationConfig(JObject.Parse(File.ReadAllText(_path)));
        }

        public void Save(StationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            File.WriteAllText(_path, config.Root.ToString(Formatting.Indented));
        }
    }
}
