using System.IO;

namespace RfpTestStation.Core
{
    public sealed class StationPaths
    {
        public StationPaths(string repoRoot)
        {
            RepoRoot = repoRoot;
        }

        public string RepoRoot { get; }

        public string ProfileHtmlPath
        {
            get { return Path.Combine(RepoRoot, "Project", "Teststand", "RFP Auto Test Sequence", "profile.html"); }
        }

        public string SequenceFilePath
        {
            get { return Path.Combine(RepoRoot, "Project", "Teststand", "RFP Auto Test Sequence", "RFP Auto Test Sequence.seq"); }
        }

        public string ConfigJsonPath
        {
            get { return Path.Combine(RepoRoot, "Runtime", "Config", "Config.json"); }
        }

        public string AppSettingsPath
        {
            get { return Path.Combine(RepoRoot, "Runtime", "Config", "AppSettings.json"); }
        }

        public string TestPlanPath
        {
            get { return Path.Combine(RepoRoot, "Runtime", "TestPlans", "Rfp7000V2.testplan.json"); }
        }

        public string MockScenariosDirectory
        {
            get { return Path.Combine(RepoRoot, "Runtime", "MockScenarios"); }
        }

        public string ReportsDirectory
        {
            get { return Path.Combine(RepoRoot, "Reports"); }
        }
    }
}
