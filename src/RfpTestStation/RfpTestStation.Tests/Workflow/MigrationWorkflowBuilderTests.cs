using System;
using System.Linq;
using RfpTestStation.Core.Importing;
using RfpTestStation.Core.Workflow;
using Xunit;

namespace RfpTestStation.Tests.Workflow
{
    public sealed class MigrationWorkflowBuilderTests
    {
        [Fact]
        public void BuildCreatesFunctionalItemsForProductionAreasFromRealProfile()
        {
            var document = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());

            var result = MigrationWorkflowBuilder.Build(document);

            Assert.Contains(result.Items, x => x.Id == "flash.mcu.simple"
                && x.Kind == TestItemKind.Flash
                && x.SourceReference.Contains("MCU简易"));
            Assert.Contains(result.Items, x => x.Id == "flash.tcon"
                && x.Kind == TestItemKind.Flash
                && x.SourceReference.Contains("TCON"));
            Assert.Contains(result.Items, x => x.Id == "flash.tddi"
                && x.Kind == TestItemKind.Flash
                && x.SourceReference.Contains("TDDI"));
            Assert.Contains(result.Items, x => x.Id == "flash.mcu.shipping"
                && x.Kind == TestItemKind.Flash
                && x.SourceReference.Contains("MCU出货"));
            Assert.Contains(result.Items, x => x.Id.StartsWith("safety.fixture-position", StringComparison.Ordinal)
                && x.Kind == TestItemKind.SafetyCheck
                && x.SourceReference.Contains("气缸到位"));
            Assert.Contains(result.Items, x => x.Id.StartsWith("fct.i2c", StringComparison.Ordinal)
                && x.Kind == TestItemKind.Measurement
                && x.SourceReference.Contains("USB2IICDll"));
            Assert.Contains(result.Items, x => x.Id.StartsWith("fct.oscilloscope", StringComparison.Ordinal)
                && x.Kind == TestItemKind.Measurement
                && x.SourceReference.Contains("Oscil"));
            Assert.Contains(result.Items, x => x.Id.StartsWith("fct.ac-input", StringComparison.Ordinal)
                && x.Kind == TestItemKind.LimitCheck
                && x.SourceReference.Contains("AC_Input"));
            Assert.Contains(result.Items, x => x.Kind == TestItemKind.Cleanup
                && x.SourceReference.Contains("/Cleanup/"));
            Assert.All(result.Items, x => Assert.False(string.IsNullOrWhiteSpace(x.SourceReference)));
        }

        [Fact]
        public void BuildSummaryListsMappedReferenceOnlyAndUnmappedCounts()
        {
            var document = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());

            var result = MigrationWorkflowBuilder.Build(document);
            var summary = result.Summary.ToText();

            Assert.Equal(820, result.Summary.ImportedStepCount);
            Assert.Equal(result.Items.Count, result.Summary.MappedFunctionalItemCount);
            Assert.True(result.Summary.ReferenceOnlyStepCount > 0);
            Assert.True(result.Summary.UnmappedProductionLookingStepCount > 0);
            Assert.Contains("Imported steps: 820", summary);
            Assert.Contains("Mapped functional items:", summary);
            Assert.Contains("Reference-only steps:", summary);
            Assert.Contains("Unmapped production-looking steps:", summary);
            Assert.NotEmpty(result.Summary.UnmappedProductionLookingSteps);
        }
    }
}
