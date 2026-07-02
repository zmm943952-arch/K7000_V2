using System.Collections.Generic;
using System.Linq;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Importing
{
    public static class StepBlockBuilder
    {
        public static IList<StepDefinition> Build(IEnumerable<StepDefinition> flatSteps)
        {
            var steps = flatSteps.ToList();
            foreach (var step in steps)
            {
                step.Children.Clear();
            }

            var roots = new List<StepDefinition>();
            var stack = new Stack<StepDefinition>();

            foreach (var step in steps)
            {
                if (step.StepType == StepType.End)
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                    }

                    continue;
                }

                if (step.StepType == StepType.Else || step.StepType == StepType.ElseIf)
                {
                    while (stack.Count > 0 && stack.Peek().StepType != StepType.If)
                    {
                        stack.Pop();
                    }

                    if (stack.Count > 0 && stack.Peek().StepType == StepType.If)
                    {
                        stack.Pop();
                    }
                }

                if (stack.Count > 0)
                {
                    stack.Peek().Children.Add(step);
                }
                else
                {
                    roots.Add(step);
                }

                if (IsBlockStep(step.StepType))
                {
                    stack.Push(step);
                }
            }

            return roots;
        }

        public static void BuildSequence(SequenceDefinition sequence)
        {
            ReplaceWithRoots(sequence.SetupSteps);
            ReplaceWithRoots(sequence.MainSteps);
            ReplaceWithRoots(sequence.CleanupSteps);
        }

        private static void ReplaceWithRoots(IList<StepDefinition> sectionSteps)
        {
            var roots = Build(sectionSteps).ToList();
            sectionSteps.Clear();
            foreach (var root in roots)
            {
                sectionSteps.Add(root);
            }
        }

        private static bool IsBlockStep(StepType stepType)
        {
            return stepType == StepType.If
                || stepType == StepType.Else
                || stepType == StepType.ElseIf
                || stepType == StepType.While
                || stepType == StepType.For
                || stepType == StepType.ForEach;
        }
    }
}
