using System;
using System.Collections.Generic;
using System.IO;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Core.TestPlans
{
    public static class TestPlanWorkflowFactory
    {
        public static IEnumerable<TestItem> CreateItems(TestPlanDefinition plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var sourceName = string.IsNullOrWhiteSpace(plan.SourcePath)
                ? plan.Name
                : Path.GetFileName(plan.SourcePath);
            foreach (var item in plan.Items)
            {
                if (!item.IsEnabled)
                {
                    continue;
                }

                yield return new TestItem(item.Id, item.Name, item.Kind)
                {
                    IsRequired = item.IsRequired,
                    StopOnFailure = item.StopOnFailure,
                    Timeout = TimeSpan.FromSeconds(item.TimeoutSeconds),
                    Parameters = item.Parameters,
                    SourceReference = string.IsNullOrWhiteSpace(item.SourceReference)
                        ? sourceName + ":" + item.Id
                        : sourceName + ":" + item.Id + " | " + item.SourceReference
                };
            }
        }
    }
}
