namespace ConfigTool
{
    using System.Collections.Generic;

    public sealed class ConfigModel
    {
        public string RfpProject1MesPath { get; set; } = "";
        public string RfpProject2MesPath { get; set; } = "";
        public string RedCaseMesPath { get; set; } = "";
        public string RedCaseBinPath { get; set; } = "";
        public List<string> RfpProject1FirmwareFiles { get; } = new List<string>();
        public List<string> RfpProject2FirmwareFiles { get; } = new List<string>();
    }
}
