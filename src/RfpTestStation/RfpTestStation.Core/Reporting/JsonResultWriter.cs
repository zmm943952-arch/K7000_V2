using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RfpTestStation.Core.Reporting
{
    public sealed class JsonResultWriter
    {
        public string Write(string directory, RunReport report)
        {
            var path = ReportFileNames.Build(directory, report, "json");
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };
            settings.Converters.Add(new StringEnumConverter());

            File.WriteAllText(path, JsonConvert.SerializeObject(report, settings));
            return path;
        }
    }
}
