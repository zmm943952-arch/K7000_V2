using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Workflow
{
    public delegate Task<TestItemResult> TestItemExecutor(
        TestItem item,
        WorkflowRunContext context,
        CancellationToken cancellationToken);

    public sealed class FunctionalTestWorkflow
    {
        private readonly IReadOnlyList<TestItem> _items;
        private readonly TestItemExecutor _executor;

        public FunctionalTestWorkflow(IEnumerable<TestItem> items, TestItemExecutor executor)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _items = items.ToList();
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public Action<TestItem>? ItemStarted { get; set; }

        public Action<TestItemResult>? ItemCompleted { get; set; }

        public async Task<WorkflowResult> RunAsync(
            WorkflowRunContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var startTime = DateTimeOffset.UtcNow;
            var results = new List<TestItemResult>();
            var status = StepStatus.Passed;
            var shouldStopNormalItems = false;

            foreach (var item in _items.Where(x => !IsFinalItem(x)))
            {
                if (shouldStopNormalItems)
                {
                    break;
                }

                var result = await ExecuteItemAsync(item, context, cancellationToken).ConfigureAwait(false);
                results.Add(result);

                if (item.IsRequired && IsBlockingStatus(result.Status))
                {
                    status = result.Status;
                    if (item.StopOnFailure)
                    {
                        shouldStopNormalItems = true;
                    }
                }

                context.Values[WorkflowRunContext.RunHasBlockingFailureKey] = status != StepStatus.Passed;
            }

            context.Values[WorkflowRunContext.RunHasBlockingFailureKey] = status != StepStatus.Passed;

            foreach (var cleanupItem in _items.Where(IsFinalItem))
            {
                var cleanupResult = await ExecuteItemAsync(cleanupItem, context, cancellationToken).ConfigureAwait(false);
                results.Add(cleanupResult);

                if (cleanupItem.IsRequired && status == StepStatus.Passed && IsBlockingStatus(cleanupResult.Status))
                {
                    status = cleanupResult.Status;
                }

                context.Values[WorkflowRunContext.RunHasBlockingFailureKey] = status != StepStatus.Passed;
            }

            return new WorkflowResult(status, results, startTime, DateTimeOffset.UtcNow);
        }

        private async Task<TestItemResult> ExecuteItemAsync(
            TestItem item,
            WorkflowRunContext context,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TestItemResult.Stopped(item, "Workflow stop requested.");
            }

            var startTime = DateTimeOffset.UtcNow;
            TestItemResult result;
            try
            {
                ItemStarted?.Invoke(item);
                result = await _executor(item, context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                result = TestItemResult.Stopped(item, "Workflow stop requested.");
            }
            catch (Exception ex)
            {
                result = TestItemResult.FromError(item, ex);
            }

            result.StartTime = result.StartTime == default(DateTimeOffset) ? startTime : result.StartTime;
            result.EndTime = result.EndTime == default(DateTimeOffset) ? DateTimeOffset.UtcNow : result.EndTime;
            ItemCompleted?.Invoke(result);
            return result;
        }

        private static bool IsBlockingStatus(StepStatus status)
        {
            return status == StepStatus.Failed ||
                   status == StepStatus.Error ||
                   status == StepStatus.Stopped ||
                   status == StepStatus.Terminated;
        }

        private static bool IsFinalItem(TestItem item)
        {
            return item.Kind == TestItemKind.ResultOutput ||
                   item.Kind == TestItemKind.Cleanup;
        }
    }
}
