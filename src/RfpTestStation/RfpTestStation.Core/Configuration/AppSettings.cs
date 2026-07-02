namespace RfpTestStation.Core.Configuration
{
    public sealed class AppSettings
    {
        public string CurrentUser { get; set; } = "Operator";

        public string ProductName { get; set; } = "K7000";

        public string SelectedLanguage { get; set; } = "中文";

        public string ExecutionMode { get; set; } = "Mock";

        public string TestPlanPath { get; set; } = "Runtime/TestPlans/Rfp7000V2.testplan.json";

        public string ConfigJsonPath { get; set; } = "Runtime/Config/Config.json";
    }
}
