using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using RfpTestStation.Core.Model;

namespace RfpTestStation.App.ViewModels
{
    public sealed class StepResultViewModel : INotifyPropertyChanged
    {
        private string _itemId = string.Empty;
        private string _stepName = string.Empty;
        private string _status = string.Empty;
        private string _statusForeground = "#1F2933";
        private string _statusBackground = "#EEF2F6";
        private string _rowBackground = "#FFFFFF";
        private string _value = string.Empty;
        private string _expectedValue = string.Empty;
        private string _compareType = string.Empty;
        private string _target = string.Empty;
        private string _limits = string.Empty;
        private string _lowLimit = string.Empty;
        private string _highLimit = string.Empty;
        private string _unit = string.Empty;
        private string _duration = string.Empty;
        private string _message = string.Empty;
        private bool _isCurrent;
        private bool _isCompleted;
        private bool _isFailure;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ItemId
        {
            get { return _itemId; }
            set { SetField(ref _itemId, value); }
        }

        public string StepName
        {
            get { return _stepName; }
            set { SetField(ref _stepName, value); }
        }

        public string Status
        {
            get { return _status; }
            set { SetField(ref _status, value); }
        }

        public string StatusForeground
        {
            get { return _statusForeground; }
            set { SetField(ref _statusForeground, value); }
        }

        public string StatusBackground
        {
            get { return _statusBackground; }
            set { SetField(ref _statusBackground, value); }
        }

        public string RowBackground
        {
            get { return _rowBackground; }
            set { SetField(ref _rowBackground, value); }
        }

        public string Value
        {
            get { return _value; }
            set { SetField(ref _value, value); }
        }

        public string ExpectedValue
        {
            get { return _expectedValue; }
            set { SetField(ref _expectedValue, value); }
        }

        public string CompareType
        {
            get { return _compareType; }
            set { SetField(ref _compareType, value); }
        }

        public string Target
        {
            get { return _target; }
            set { SetField(ref _target, value); }
        }

        public string Limits
        {
            get { return _limits; }
            set { SetField(ref _limits, value); }
        }

        public string LowLimit
        {
            get { return _lowLimit; }
            set { SetField(ref _lowLimit, value); }
        }

        public string HighLimit
        {
            get { return _highLimit; }
            set { SetField(ref _highLimit, value); }
        }

        public string Unit
        {
            get { return _unit; }
            set { SetField(ref _unit, value); }
        }

        public string Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetField(ref _message, value); }
        }

        public bool IsCurrent
        {
            get { return _isCurrent; }
            private set { SetField(ref _isCurrent, value); }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            private set { SetField(ref _isCompleted, value); }
        }

        public bool IsFailure
        {
            get { return _isFailure; }
            private set { SetField(ref _isFailure, value); }
        }

        public static StepResultViewModel Pending(string itemId, string stepName)
        {
            return new StepResultViewModel
            {
                ItemId = itemId,
                StepName = stepName,
                Status = "Pending",
                StatusForeground = "#475467",
                StatusBackground = "#F2F4F7",
                RowBackground = "#FFFFFF"
            };
        }

        public static StepResultViewModel FromResult(StepResult result)
        {
            var viewModel = new StepResultViewModel();
            viewModel.MarkCompleted(result);
            return viewModel;
        }

        public void MarkRunning()
        {
            Status = "Running";
            StatusForeground = "#075985";
            StatusBackground = "#EFF8FF";
            RowBackground = "#F0F9FF";
            IsCurrent = true;
            IsCompleted = false;
            IsFailure = false;
        }

        public void MarkCompleted(StepResult result)
        {
            StepName = result.StepName;
            Status = result.Status.ToString();
            StatusForeground = StatusColor.Foreground(result.Status);
            StatusBackground = StatusColor.Background(result.Status);
            RowBackground = "#FFFFFF";
            Value = Convert.ToString(result.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            ExpectedValue = Convert.ToString(result.ExpectedValue, CultureInfo.InvariantCulture) ?? string.Empty;
            CompareType = result.CompareType ?? string.Empty;
            Target = result.Target ?? string.Empty;
            Limits = FormatLimits(result);
            LowLimit = result.LowLimit?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty;
            HighLimit = result.HighLimit?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty;
            Unit = result.Unit ?? string.Empty;
            Duration = FormatDuration(result);
            Message = result.Message ?? result.Error?.Message ?? string.Empty;
            IsCurrent = false;
            IsCompleted = true;
            IsFailure = IsFailureStatus(result.Status);
        }

        private static bool IsFailureStatus(StepStatus status)
        {
            return status == StepStatus.Failed
                || status == StepStatus.Error
                || status == StepStatus.Stopped
                || status == StepStatus.Terminated;
        }

        private static string FormatLimits(StepResult result)
        {
            if (result.LowLimit == null && result.HighLimit == null)
            {
                return string.Empty;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}..{1}",
                result.LowLimit?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty,
                result.HighLimit?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty);
        }

        private static string FormatDuration(StepResult result)
        {
            if (result.StartTime == default || result.EndTime == default)
            {
                return string.Empty;
            }

            return (result.EndTime - result.StartTime).TotalSeconds.ToString("0.000s", CultureInfo.InvariantCulture);
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static class StatusColor
        {
            public static string Foreground(StepStatus status)
            {
                switch (status)
                {
                    case StepStatus.Passed:
                        return "#166534";
                    case StepStatus.Failed:
                        return "#991B1B";
                    case StepStatus.Error:
                        return "#9A3412";
                    case StepStatus.Skipped:
                    case StepStatus.Stopped:
                    case StepStatus.Terminated:
                        return "#475467";
                    default:
                        return "#075985";
                }
            }

            public static string Background(StepStatus status)
            {
                switch (status)
                {
                    case StepStatus.Passed:
                        return "#ECFDF3";
                    case StepStatus.Failed:
                        return "#FEF2F2";
                    case StepStatus.Error:
                        return "#FFF7ED";
                    case StepStatus.Skipped:
                    case StepStatus.Stopped:
                    case StepStatus.Terminated:
                        return "#F2F4F7";
                    default:
                        return "#EFF8FF";
                }
            }
        }
    }
}
