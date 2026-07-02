using System;
using System.Linq;
using RfpTestStation.Core.Importing;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.Importing
{
    public sealed class ProfileHtmlImporterTests
    {
        private static readonly Lazy<SequenceDocument> ImportedDocument =
            new Lazy<SequenceDocument>(() => ProfileHtmlImporter.Load(TestPaths.ProfileHtml()));

        [Fact]
        public void LoadImportsSequenceAndStepCountsFromExportedProfile()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");

            Assert.Equal(12, document.Sequences.Count);
            Assert.Equal(820, document.Sequences.Sum(x => x.AllSteps.Count));
            Assert.Equal(671, main.AllSteps.Count);
            Assert.Equal(16, main.SetupSteps.Count);
            Assert.Equal(627, main.MainSteps.Count);
            Assert.Equal(28, main.CleanupSteps.Count);
        }

        [Fact]
        public void LoadPreservesBurnStepNames()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var names = main.AllSteps.Select(x => x.Name).ToList();

            Assert.Contains("MCU简易", names);
            Assert.Contains("TCON", names);
            Assert.Contains("TDDI", names);
            Assert.Contains("MCU出货", names);
        }

        [Fact]
        public void LoadParsesBasicStepFields()
        {
            var document = ImportedDocument.Value;
            var firstSetup = document.GetSequence("MainSequence").SetupSteps.First();

            Assert.Equal("Post MainSeq ThisContext", firstSetup.Name);
            Assert.Equal("Action", firstSetup.StepTypeRaw);
            Assert.Equal("ActiveX/COM", firstSetup.AdapterName);
            Assert.Equal("Setup", firstSetup.SectionName);
        }

        [Fact]
        public void LoadImportsSequenceFileGlobals()
        {
            var document = ImportedDocument.Value;

            Assert.Equal(false, document.FileGlobals["StopStatus"]);
            Assert.Equal(@"D:\Project\Config.json", document.FileGlobals["Json_FilePath"]);
            Assert.Equal(0.0, document.FileGlobals["stepNums"]);
        }

        [Fact]
        public void LoadImportsSequenceLocals()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");

            Assert.Equal(false, main.Locals["X1气缸原点"]);
            Assert.Equal(true, main.Locals["bSendResponse"]);
            Assert.Equal(0.0, main.Locals["i"]);
        }

        [Fact]
        public void LoadParsesNewThreadSequenceCall()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var stopMonitor = main.AllSteps.First(x => x.Name == "Stop_Seq_Period");

            Assert.True(stopMonitor.IsNewThread);
        }

        [Fact]
        public void LoadDoesNotIncludeAdditionalResultTextInPreExpression()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var step = main.AllSteps.First(x => x.Name == "TMP1_PHSA_0V Received Value");

            Assert.DoesNotContain("Records", step.PreExpression ?? string.Empty);
        }

        [Fact]
        public void LoadParsesNumericLimitDataSourceAndLimits()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var step = main.AllSteps.First(x => x.Name == "Read AC_Input3 Limit Test_AUTO_IND_HI");

            Assert.Equal("Locals.AC_Input3", step.ConditionExpression);
            Assert.Equal(3.135, step.Limits.Single().Low);
            Assert.Equal(3.465, step.Limits.Single().High);
        }

        [Fact]
        public void LoadMapsNiMultipleNumericLimitTest()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var step = main.AllSteps.First(x => x.Name == "DEF_FRT_SW_LO Receive Value");

            Assert.Equal(StepType.MultipleNumericLimitTest, step.StepType);
        }

        [Fact]
        public void LoadDoesNotLeakFlowPropertiesAcrossAdjacentSteps()
        {
            var document = ImportedDocument.Value;
            var main = document.GetSequence("MainSequence");
            var flash = main.AllSteps.First(x => (x.DescriptionRaw ?? string.Empty).Contains("Test RFP_Flash_once.bat.vi"));
            var readVolt = main.AllSteps.First(x => x.Name == "Read AC_Input3"
                && (x.DescriptionRaw ?? string.Empty).Contains("ReadVolt.vi"));

            Assert.Equal(RunMode.Normal, flash.RunMode);
            Assert.Equal(RunMode.Normal, readVolt.RunMode);
            Assert.DoesNotContain("Step: Read AC_Input3 Limit", readVolt.FlowPropertiesRaw ?? string.Empty);
            Assert.DoesNotContain("Run Mode: Skip", readVolt.FlowPropertiesRaw ?? string.Empty);
        }
    }
}
