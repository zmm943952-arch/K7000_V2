using System.Collections.ObjectModel;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.App.ViewModels
{
    public sealed class StepTreeNodeViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string StepType { get; set; } = string.Empty;

        public ObservableCollection<StepTreeNodeViewModel> Children { get; } = new ObservableCollection<StepTreeNodeViewModel>();

        public static StepTreeNodeViewModel FromStep(StepDefinition step)
        {
            var node = new StepTreeNodeViewModel
            {
                Name = step.Name,
                StepType = step.StepType.ToString()
            };

            foreach (var child in step.Children)
            {
                node.Children.Add(FromStep(child));
            }

            return node;
        }

        public static StepTreeNodeViewModel FromTestItem(TestItem item)
        {
            return new StepTreeNodeViewModel
            {
                Name = item.Name,
                StepType = item.Kind.ToString()
            };
        }
    }
}
