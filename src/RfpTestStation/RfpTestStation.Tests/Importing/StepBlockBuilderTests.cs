using System.Linq;
using RfpTestStation.Core.Importing;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.Importing
{
    public sealed class StepBlockBuilderTests
    {
        [Fact]
        public void BuildCreatesNestedBlocksFromIndentation()
        {
            var steps = new[]
            {
                Step("If", StepType.If, 0),
                Step("Inside If", StepType.Action, 1),
                Step("Else", StepType.Else, 0),
                Step("While", StepType.While, 1),
                Step("Inside While", StepType.Action, 2),
                Step("End While", StepType.End, 1),
                Step("End If", StepType.End, 0),
                Step("After", StepType.Action, 0)
            };

            var roots = StepBlockBuilder.Build(steps);

            Assert.Equal(new[] { "If", "Else", "After" }, roots.Select(x => x.Name).ToArray());
            Assert.Equal("Inside If", roots[0].Children.Single().Name);
            Assert.Equal("While", roots[1].Children.Single().Name);
            Assert.Equal("Inside While", roots[1].Children.Single().Children.Single().Name);
        }

        [Fact]
        public void BuildSequenceKeepsFlatStepsAndExposesRootStepsBySection()
        {
            var sequence = new SequenceDefinition { Name = "Synthetic" };
            sequence.MainSteps.Add(Step("While", StepType.While, 0));
            sequence.MainSteps.Add(Step("Action", StepType.Action, 1));
            sequence.MainSteps.Add(Step("End", StepType.End, 0));
            foreach (var step in sequence.MainSteps)
            {
                sequence.AllSteps.Add(step);
            }

            StepBlockBuilder.BuildSequence(sequence);

            Assert.Equal(3, sequence.AllSteps.Count);
            Assert.Single(sequence.MainSteps);
            Assert.Equal("Action", sequence.MainSteps[0].Children.Single().Name);
        }

        [Fact]
        public void ImportedProfileContainsNestedX2WaitLoop()
        {
            var document = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());
            var main = document.GetSequence("MainSequence");

            StepBlockBuilder.BuildSequence(main);

            var x2Loop = main.MainSteps
                .SelectMany(Flatten)
                .First(x => x.StepType == StepType.While && (x.DescriptionRaw ?? string.Empty).Contains("!Locals.X2"));

            Assert.Contains(x2Loop.Children, x => x.Name.StartsWith("Read X2-"));
        }

        [Fact]
        public void ImportedProfileContainsNestedX6PushLoop()
        {
            var document = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());
            var main = document.GetSequence("MainSequence");

            StepBlockBuilder.BuildSequence(main);

            var x6Loop = main.MainSteps
                .SelectMany(Flatten)
                .First(x => x.StepType == StepType.While && (x.DescriptionRaw ?? string.Empty).Contains("!Locals.X6"));

            Assert.Contains(x6Loop.Children, x => x.Name.StartsWith("Read X6-"));
        }

        private static StepDefinition Step(string name, StepType type, int indentLevel)
        {
            return new StepDefinition
            {
                Name = name,
                StepType = type,
                IndentLevel = indentLevel
            };
        }

        private static StepDefinition[] Flatten(StepDefinition step)
        {
            return new[] { step }
                .Concat(step.Children.SelectMany(Flatten))
                .ToArray();
        }
    }
}
