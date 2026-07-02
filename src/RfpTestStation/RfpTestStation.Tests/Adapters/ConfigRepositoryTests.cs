using System;
using System.IO;
using Newtonsoft.Json.Linq;
using RfpTestStation.Adapters.Config;
using Xunit;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class ConfigRepositoryTests
    {
        [Fact]
        public void SavePreservesUnknownFields()
        {
            var path = Path.Combine(Path.GetTempPath(), "RfpTestStation_Config_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "{ \"Known\": { \"Value\": \"old\" }, \"Unknown\": { \"Nested\": 42 } }");
                var repository = new ConfigRepository(path);

                var config = repository.Load();
                config.SetValue("Known.Value", "new");
                repository.Save(config);

                var saved = JObject.Parse(File.ReadAllText(path));
                Assert.Equal("new", (string)saved.SelectToken("Known.Value")!);
                Assert.Equal(42, (int)saved.SelectToken("Unknown.Nested")!);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
