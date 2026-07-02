using System.IO;
using Xunit;

namespace RfpTestStation.Tests.App
{
    public sealed class DeploymentPackageTests
    {
        [Fact]
        public void PackageReleaseScriptBuildsSingleFolderDeployment()
        {
            var scriptPath = Path.Combine(TestPaths.RepoRoot(), "Tools", "package-release.ps1");

            Assert.True(File.Exists(scriptPath));
            var script = File.ReadAllText(scriptPath);
            Assert.Contains("Runtime", script);
            Assert.Contains("Reports", script);
            Assert.Contains("RfpTestStation.App.exe", script);
            Assert.DoesNotContain("Project\\Reports", script);
            Assert.DoesNotContain("Project/AppSettings.json", script);
        }
    }
}
