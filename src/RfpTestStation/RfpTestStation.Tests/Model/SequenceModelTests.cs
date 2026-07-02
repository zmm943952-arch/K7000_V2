using System;
using System.IO;
using RfpTestStation.Core;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.Model
{
    public sealed class SequenceModelTests
    {
        [Fact]
        public void StationPathsResolveRuntimeAssetsFromApplicationRoot()
        {
            var paths = new StationPaths(TestPaths.RepoRoot());

            Assert.True(File.Exists(paths.ProfileHtmlPath));
            Assert.True(File.Exists(paths.ConfigJsonPath));
            Assert.EndsWith(Path.Combine("Runtime", "Config", "Config.json"), paths.ConfigJsonPath);
            Assert.EndsWith(Path.Combine("Runtime", "Config", "AppSettings.json"), paths.AppSettingsPath);
            Assert.EndsWith(Path.Combine("Runtime", "TestPlans", "Rfp7000V2.testplan.json"), paths.TestPlanPath);
            Assert.EndsWith("Reports", paths.ReportsDirectory);
            Assert.DoesNotContain(Path.DirectorySeparatorChar + "Project" + Path.DirectorySeparatorChar, paths.AppSettingsPath);
            Assert.DoesNotContain(Path.DirectorySeparatorChar + "Project" + Path.DirectorySeparatorChar, paths.ReportsDirectory);
        }

        [Fact]
        public void SequenceDocumentReturnsSequenceByName()
        {
            var document = new SequenceDocument();
            var main = new SequenceDefinition { Name = "MainSequence" };

            document.Sequences.Add(main);

            Assert.Same(main, document.GetSequence("MainSequence"));
        }

        [Fact]
        public void SequenceDocumentThrowsForMissingSequence()
        {
            var document = new SequenceDocument();

            Assert.Throws<InvalidOperationException>(() => document.GetSequence("Missing"));
        }
    }
}
