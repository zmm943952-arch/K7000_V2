using System;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using RfpTestStation.App.ViewModels;

namespace RfpTestStation.App
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private readonly DispatcherTimer _clockTimer;

        public MainWindow()
        {
            InitializeComponent();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (_, __) => _viewModel?.RefreshCurrentTime(DateTime.Now);
            AttachViewModel(new MainViewModel());
            StartClockTimer();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModelPropertyChanged;
            }

            _clockTimer.Stop();
            base.OnClosed(e);
        }

        private void AttachViewModel(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            ApplyLocalizedHeaders(viewModel);
        }

        private void StartClockTimer()
        {
            _viewModel?.RefreshCurrentTime(DateTime.Now);
            _clockTimer.Start();
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            if (e.PropertyName == nameof(MainViewModel.SelectedLanguage)
                || e.PropertyName == nameof(MainViewModel.StepHeaderText)
                || e.PropertyName == nameof(MainViewModel.EnabledHeaderText)
                || e.PropertyName == nameof(MainViewModel.RequiredHeaderText)
                || e.PropertyName == nameof(MainViewModel.StopOnFailureHeaderText)
                || e.PropertyName == nameof(MainViewModel.TimeoutHeaderText)
                || e.PropertyName == nameof(MainViewModel.KindHeaderText)
                || e.PropertyName == nameof(MainViewModel.ScriptHeaderText)
                || e.PropertyName == nameof(MainViewModel.AdapterHeaderText)
                || e.PropertyName == nameof(MainViewModel.OperationHeaderText)
                || e.PropertyName == nameof(MainViewModel.NoteHeaderText)
                || e.PropertyName == nameof(MainViewModel.SummaryHeaderText)
                || e.PropertyName == nameof(MainViewModel.DescriptionHeaderText)
                || e.PropertyName == nameof(MainViewModel.StatusHeaderText)
                || e.PropertyName == nameof(MainViewModel.MeasurementHeaderText)
                || e.PropertyName == nameof(MainViewModel.ExpectedValueHeaderText)
                || e.PropertyName == nameof(MainViewModel.TargetHeaderText)
                || e.PropertyName == nameof(MainViewModel.UnitsHeaderText)
                || e.PropertyName == nameof(MainViewModel.LowLimitHeaderText)
                || e.PropertyName == nameof(MainViewModel.HighLimitHeaderText)
                || e.PropertyName == nameof(MainViewModel.ComparisonTypeHeaderText))
            {
                ApplyLocalizedHeaders(_viewModel);
            }
        }

        private void ApplyLocalizedHeaders(MainViewModel viewModel)
        {
            MainStepColumn.Header = viewModel.StepHeaderText;
            MainDescriptionColumn.Header = viewModel.DescriptionHeaderText;
            MainStatusColumn.Header = viewModel.StatusHeaderText;

            DetailStepColumn.Header = viewModel.StepHeaderText;
            DetailStatusColumn.Header = viewModel.StatusHeaderText;
            DetailMeasurementColumn.Header = viewModel.MeasurementHeaderText;
            DetailExpectedValueColumn.Header = viewModel.ExpectedValueHeaderText;
            DetailUnitsColumn.Header = viewModel.UnitsHeaderText;
            DetailTargetColumn.Header = viewModel.TargetHeaderText;
            DetailComparisonTypeColumn.Header = viewModel.ComparisonTypeHeaderText;

            PlanEnabledColumn.Header = viewModel.EnabledHeaderText;
            PlanIdColumn.Header = "ID";
            PlanNameColumn.Header = viewModel.StepHeaderText;
            PlanKindColumn.Header = viewModel.KindHeaderText;
            PlanRequiredColumn.Header = viewModel.RequiredHeaderText;
            PlanStopOnFailureColumn.Header = viewModel.StopOnFailureHeaderText;
            PlanTimeoutColumn.Header = viewModel.TimeoutHeaderText;
            PlanSummaryColumn.Header = viewModel.SummaryHeaderText;
            PlanScriptColumn.Header = viewModel.ScriptHeaderText;
            PlanAdapterColumn.Header = viewModel.AdapterHeaderText;
            PlanOperationColumn.Header = viewModel.OperationHeaderText;
            PlanLowLimitColumn.Header = viewModel.LowLimitHeaderText;
            PlanHighLimitColumn.Header = viewModel.HighLimitHeaderText;
            PlanUnitColumn.Header = viewModel.UnitsHeaderText;
            PlanComparisonTypeColumn.Header = viewModel.ComparisonTypeHeaderText;
            PlanNoteColumn.Header = viewModel.NoteHeaderText;
        }
    }
}
