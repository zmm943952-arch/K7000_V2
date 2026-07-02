using System;
using System.IO;
using ConfigTool;
using Newtonsoft.Json.Linq;

namespace ConfigTool.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                return Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message);
                return 1;
            }
        }

        private static int Run()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "ConfigToolTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var configPath = Path.Combine(tempRoot, "Config.json");
                File.WriteAllText(configPath, @"{
  ""Burn1"": {
    ""Name"": ""RFP烧录"",
    ""Enabled"": true,
    ""Params"": {
      ""FilePath"": "".\\RFP_Auto\\Scripts\\Flash_once.bat"",
      ""Porject1_Name"": ""k7000_1.rpj"",
      ""Porject2_Name"": ""k7000_2.rpj""
    }
  },
  ""Burn2"": {
    ""Name"": ""TCON烧录"",
    ""Enabled"": true,
    ""Params"": {
      ""FilePath"": "".\\RedCase_Auto\\Debug\\FlashUpdate_Run.bat"",
      ""BinFilePath"": "".\\RedCase_Auto\\Debug\\old.bin""
    }
  }
}");

                var service = new ConfigService(configPath);
                var model = service.Load();
                model.RfpProject1MesPath = @"\\mes\rfp\k7000_1";
                model.RfpProject2MesPath = @"\\mes\rfp\k7000_2";
                model.RedCaseMesPath = @"\\mes\redcase";
                model.RedCaseBinPath = @"\\mes\redcase\panel.bin";
                model.RfpProject1FirmwareFiles.Add(@"\\mes\rfp\k7000_1\app.mot");
                model.RfpProject1FirmwareFiles.Add(@"\\mes\rfp\k7000_1\boot.mot");

                service.Save(model);

                var token = JObject.Parse(File.ReadAllText(configPath));
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][0]["ProjectName"], "k7000_1.rpj", "RFP project 1 name");
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][0]["MesFirmwarePath"], @"\\mes\rfp\k7000_1", "RFP project 1 MES path");
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][0]["LocalFirmwarePath"], @".\RFP_Auto\Firmware\k7000_1", "RFP project 1 local path");
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][0]["FirmwareFiles"][0]["MesFileName"], "app.mot", "RFP project 1 file 0");
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][0]["FirmwareFiles"][1]["MesFileName"], "boot.mot", "RFP project 1 file 1");

                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][1]["ProjectName"], "k7000_2.rpj", "RFP project 2 name");
                AssertEqual((string)token["Burn1"]["Params"]["RfpProjects"][1]["MesFirmwarePath"], @"\\mes\rfp\k7000_2", "RFP project 2 MES path");
                AssertEqual(((JArray)token["Burn1"]["Params"]["RfpProjects"][1]["FirmwareFiles"]).Count.ToString(), "0", "RFP project 2 empty FirmwareFiles");
                AssertEqual((string)token["Burn2"]["Params"]["MesFirmwarePath"], @"\\mes\redcase", "RedCase MES path");
                AssertEqual((string)token["Burn2"]["Params"]["LocalFirmwarePath"], @".\RedCase_Auto\Debug\Firmware_Local", "RedCase local path");
                AssertEqual((string)token["Burn2"]["Params"]["BinFilePath"], @"\\mes\redcase\panel.bin", "RedCase bin path");
                AssertTrue(token["Burn2"]["Params"]["FirmwareFiles"] is JArray, "RedCase FirmwareFiles array");

                var reloaded = service.Load();
                AssertEqual(reloaded.RfpProject1FirmwareFiles.Count.ToString(), "2", "Reloaded RFP project 1 file count");
                AssertEqual(reloaded.RfpProject1FirmwareFiles[0], @"\\mes\rfp\k7000_1\app.mot", "Reloaded RFP project 1 file 0 path");
                AssertEqual(reloaded.RfpProject1FirmwareFiles[1], @"\\mes\rfp\k7000_1\boot.mot", "Reloaded RFP project 1 file 1 path");
                AssertEqual(reloaded.RfpProject2FirmwareFiles.Count.ToString(), "0", "Reloaded RFP project 2 file count");

                Console.WriteLine("ConfigTool tests passed.");
                return 0;
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        private static void AssertEqual(string actual, string expected, string label)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(label + " expected [" + expected + "] actual [" + actual + "]");
            }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (!condition)
            {
                throw new InvalidOperationException(label + " expected true");
            }
        }
    }
}
