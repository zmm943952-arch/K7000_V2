using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RfpTestStation.Adapters.Config;
using RfpTestStation.Adapters.Hardware;
using RfpTestStation.Adapters.Mock;
using RfpTestStation.Adapters.TestPlans;
using RfpTestStation.Core;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Configuration;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.MockScenarios;
using RfpTestStation.Core.Preflight;
using RfpTestStation.Core.Reporting;
using RfpTestStation.Core.Safety;
using RfpTestStation.Core.TestPlans;
using RfpTestStation.Core.Validation;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.App.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private const string NoMockScenarioName = "None";
        private readonly RelayCommand _startCommand;
        private readonly RelayCommand _stopCommand;
        private readonly RelayCommand _resetCommand;
        private readonly RelayCommand _saveSettingsCommand;
        private readonly RelayCommand _saveStationConfigCommand;
        private readonly RelayCommand _reloadStationConfigCommand;
        private readonly RelayCommand _saveTestPlanCommand;
        private readonly RelayCommand _reloadTestPlanCommand;
        private readonly RelayCommand _showRunPageCommand;
        private readonly RelayCommand _showTestPlanPageCommand;
        private readonly RelayCommand _showSettingsPageCommand;
        private readonly RelayCommand _selectSettingsSectionCommand;
        private readonly string _repoRoot;
        private readonly StationPaths _stationPaths;
        private readonly AppSettingsRepository _settingsRepository;
        private CancellationTokenSource? _runCancellation;
        private bool _isInitializing;
        private string _serialNumber = string.Empty;
        private string _executionMode = "Mock";
        private string _selectedMockScenarioName = NoMockScenarioName;
        private string _selectedLanguage = "中文";
        private string _overallStatus = "Idle";
        private string _overallStatusReason = string.Empty;
        private string _currentStep = "-";
        private string _progressText = "0 / 0";
        private double _progressPercent;
        private string _currentUser = "Operator";
        private string _productName = "K7000";
        private string _testPlanName = "RFP 7000 V2";
        private string _configName = "Config.json";
        private string _testPlanPath = "Runtime/TestPlans/Rfp7000V2.testplan.json";
        private string _configJsonPath = "Runtime/Config/Config.json";
        private string _settingsStatusText = string.Empty;
        private string _stationConfigStatusText = string.Empty;
        private string _stationConfigValidationText = string.Empty;
        private bool _hasStationConfigValidationError;
        private string _testPlanEditorStatusText = string.Empty;
        private string _currentPage = "Run";
        private string _selectedSettingsSection = "Common";
        private string _editableTestPlanName = string.Empty;
        private string _editableTestPlanVersion = string.Empty;
        private string _editableTestPlanProduct = string.Empty;
        private TestPlanItemEditorViewModel? _selectedTestPlanItem;
        private bool _hasTestPlanValidationIssues;
        private string _rfpFlashScriptPath = string.Empty;
        private string _tconFlashScriptPath = string.Empty;
        private string _tconBinFilePath = string.Empty;
        private string _tddiFlashScriptPath = string.Empty;
        private string _tddiSerialPort = string.Empty;
        private string _ioDaqCom = string.Empty;
        private string _scannerCom = string.Empty;
        private string _oscilloscopeHost = string.Empty;
        private string _fctStation = string.Empty;
        private bool _safetyEnabled = true;
        private int _safetyPollIntervalMs = 50;
        private int _emergencyStopChannel = 4;
        private bool _emergencyStopTriggeredValue = true;
        private int _lightCurtainChannel = 5;
        private bool _lightCurtainTriggeredValue = false;
        private int _fixtureDownOutputChannel = 2;
        private int _fixtureUpOutputChannel = 1;
        private int _fixtureUpDelayMs = 1000;
        private string _currentTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        private string _runElapsedSecondsText = "0";
        private int _goodCount;
        private int _failCount;
        private int _errorCount;
        private int _totalCount;
        private string _yieldRateText = "0.00%";
        private double _yieldRatePercent;
        private bool _isRunning;
        private readonly RelayCommand _resetStatisticsCommand;

        public MainViewModel()
            : this(FindRepoRoot())
        {
        }

        public MainViewModel(string repoRoot)
        {
            _isInitializing = true;
            _repoRoot = repoRoot;
            _stationPaths = new StationPaths(repoRoot);
            _settingsRepository = new AppSettingsRepository(_stationPaths.AppSettingsPath, repoRoot);
            _startCommand = new RelayCommand(_ => StartRun(), _ => !IsRunning);
            _stopCommand = new RelayCommand(_ => StopRun(), _ => IsRunning);
            _resetCommand = new RelayCommand(_ => Reset());
            _resetStatisticsCommand = new RelayCommand(_ => ResetStatistics());
            _saveSettingsCommand = new RelayCommand(_ => SaveSettings(), _ => !IsRunning);
            _saveStationConfigCommand = new RelayCommand(_ => SaveStationConfig(), _ => !IsRunning);
            _reloadStationConfigCommand = new RelayCommand(_ => LoadStationConfigEditor(), _ => !IsRunning);
            _saveTestPlanCommand = new RelayCommand(_ => SaveTestPlan(), _ => !IsRunning);
            _reloadTestPlanCommand = new RelayCommand(_ => LoadTestPlanEditor(), _ => !IsRunning);
            _showRunPageCommand = new RelayCommand(_ => CurrentPage = "Run");
            _showTestPlanPageCommand = new RelayCommand(_ => CurrentPage = "TestPlan");
            _showSettingsPageCommand = new RelayCommand(_ => CurrentPage = "Settings");
            _selectSettingsSectionCommand = new RelayCommand(parameter => SelectedSettingsSection = Convert.ToString(parameter, CultureInfo.InvariantCulture) ?? "Common");

            foreach (var placeholder in HardwareStationAdapterRegistry.LabViewPlaceholders)
            {
                PlaceholderItems.Add(placeholder);
            }

            LoadMockScenarioOptions();
            ApplySettings(_settingsRepository.LoadOrDefault(_stationPaths));
            LoadStationConfigEditor();
            LoadTestPlanEditor();
            _isInitializing = false;
            RunAutomaticHardwareSelfCheck();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SerialNumber
        {
            get { return _serialNumber; }
            set { SetField(ref _serialNumber, value); }
        }

        public string ExecutionMode
        {
            get { return _executionMode; }
            set
            {
                if (SetField(ref _executionMode, value))
                {
                    OnPropertyChanged(nameof(ExecutionModeStatusText));
                    OnPropertyChanged(nameof(IsHardwareMode));
                    OnPropertyChanged(nameof(HardwareModeWarningVisibility));
                    OnPropertyChanged(nameof(HardwareModeWarningText));
                    OnPropertyChanged(nameof(MockScenarioVisibility));
                    if (!_isInitializing)
                    {
                        RunAutomaticHardwareSelfCheck();
                    }
                }
            }
        }

        public string SelectedLanguage
        {
            get { return _selectedLanguage; }
            set
            {
                if (SetField(ref _selectedLanguage, value))
                {
                    RaiseLocalizedPropertiesChanged();
                }
            }
        }

        public string OverallStatus
        {
            get { return _overallStatus; }
            private set
            {
                if (SetField(ref _overallStatus, value))
                {
                    OnPropertyChanged(nameof(OverallStatusText));
                }
            }
        }

        private string OverallStatusReason
        {
            get { return _overallStatusReason; }
            set
            {
                if (SetField(ref _overallStatusReason, value))
                {
                    OnPropertyChanged(nameof(OverallStatusText));
                }
            }
        }

        public string OverallStatusText
        {
            get
            {
                var statusText = TranslateStatus(OverallStatus);
                return string.IsNullOrWhiteSpace(OverallStatusReason)
                    ? statusText
                    : statusText + ": " + OverallStatusReason.Trim();
            }
        }

        public string CurrentStep
        {
            get { return _currentStep; }
            private set { SetField(ref _currentStep, value); }
        }

        public string ProgressText
        {
            get { return _progressText; }
            private set { SetField(ref _progressText, value); }
        }

        public double ProgressPercent
        {
            get { return _progressPercent; }
            private set { SetField(ref _progressPercent, value); }
        }

        public string CurrentUser
        {
            get { return _currentUser; }
            set { SetField(ref _currentUser, value); }
        }

        public string ProductName
        {
            get { return _productName; }
            set { SetField(ref _productName, NormalizeProductName(value)); }
        }

        public string SelectedMockScenarioName
        {
            get { return _selectedMockScenarioName; }
            set { SetField(ref _selectedMockScenarioName, NormalizeMockScenarioName(value)); }
        }

        public string TestPlanName
        {
            get { return _testPlanName; }
            private set { SetField(ref _testPlanName, value); }
        }

        public string ConfigName
        {
            get { return _configName; }
            private set { SetField(ref _configName, value); }
        }

        public string TestPlanPath
        {
            get { return _testPlanPath; }
            set
            {
                if (SetField(ref _testPlanPath, value))
                {
                    RefreshConfiguredFileNames();
                }
            }
        }

        public string ConfigJsonPath
        {
            get { return _configJsonPath; }
            set
            {
                if (SetField(ref _configJsonPath, value))
                {
                    RefreshConfiguredFileNames();
                }
            }
        }

        public string SettingsStatusText
        {
            get { return _settingsStatusText; }
            private set { SetField(ref _settingsStatusText, value); }
        }

        public string StationConfigStatusText
        {
            get { return _stationConfigStatusText; }
            private set { SetField(ref _stationConfigStatusText, value); }
        }

        public string StationConfigValidationText
        {
            get { return _stationConfigValidationText; }
            private set { SetField(ref _stationConfigValidationText, value); }
        }

        public bool HasStationConfigValidationError
        {
            get { return _hasStationConfigValidationError; }
            private set
            {
                if (SetField(ref _hasStationConfigValidationError, value))
                {
                    OnPropertyChanged(nameof(StationConfigValidationVisibility));
                }
            }
        }

        public Visibility StationConfigValidationVisibility
        {
            get { return HasStationConfigValidationError ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string TestPlanEditorStatusText
        {
            get { return _testPlanEditorStatusText; }
            private set { SetField(ref _testPlanEditorStatusText, value); }
        }

        public string CurrentPage
        {
            get { return _currentPage; }
            set
            {
                if (SetField(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(IsRunPage));
                    OnPropertyChanged(nameof(IsTestPlanPage));
                    OnPropertyChanged(nameof(IsSettingsPage));
                    OnPropertyChanged(nameof(RunPageVisibility));
                    OnPropertyChanged(nameof(TestPlanPageVisibility));
                    OnPropertyChanged(nameof(SettingsPageVisibility));
                }
            }
        }

        public bool IsRunPage
        {
            get { return string.Equals(CurrentPage, "Run", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsSettingsPage
        {
            get { return string.Equals(CurrentPage, "Settings", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsTestPlanPage
        {
            get { return string.Equals(CurrentPage, "TestPlan", StringComparison.OrdinalIgnoreCase); }
        }

        public Visibility RunPageVisibility
        {
            get { return IsRunPage ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility TestPlanPageVisibility
        {
            get { return IsTestPlanPage ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility SettingsPageVisibility
        {
            get { return IsSettingsPage ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string SelectedSettingsSection
        {
            get { return _selectedSettingsSection; }
            set
            {
                var section = string.IsNullOrWhiteSpace(value) ? "Common" : value.Trim();
                if (SetField(ref _selectedSettingsSection, section))
                {
                    RaiseSettingsSectionPropertiesChanged();
                }
            }
        }

        public bool IsCommonSettingsSection { get { return IsSettingsSection("Common"); } }

        public bool IsStartupSettingsSection { get { return IsSettingsSection("Startup"); } }

        public bool IsFlashingSettingsSection { get { return IsSettingsSection("Flashing"); } }

        public bool IsInstrumentSettingsSection { get { return IsSettingsSection("Instruments"); } }

        public bool IsFctSettingsSection { get { return IsSettingsSection("FCT"); } }

        public bool IsSafetySettingsSection { get { return IsSettingsSection("Safety"); } }

        public bool IsAdvancedSettingsSection { get { return IsSettingsSection("Advanced"); } }

        public Visibility CommonSettingsSectionVisibility { get { return SectionVisibility("Common"); } }

        public Visibility StartupSettingsSectionVisibility { get { return SectionVisibility("Startup"); } }

        public Visibility FlashingSettingsSectionVisibility { get { return SectionVisibility("Flashing"); } }

        public Visibility InstrumentSettingsSectionVisibility { get { return SectionVisibility("Instruments"); } }

        public Visibility FctSettingsSectionVisibility { get { return SectionVisibility("FCT"); } }

        public Visibility SafetySettingsSectionVisibility { get { return SectionVisibility("Safety"); } }

        public Visibility AdvancedSettingsSectionVisibility { get { return SectionVisibility("Advanced"); } }

        public string EditableTestPlanName
        {
            get { return _editableTestPlanName; }
            set { SetField(ref _editableTestPlanName, value); }
        }

        public string EditableTestPlanVersion
        {
            get { return _editableTestPlanVersion; }
            set { SetField(ref _editableTestPlanVersion, value); }
        }

        public string EditableTestPlanProduct
        {
            get { return _editableTestPlanProduct; }
            set { SetField(ref _editableTestPlanProduct, value); }
        }

        public TestPlanItemEditorViewModel? SelectedTestPlanItem
        {
            get { return _selectedTestPlanItem; }
            set
            {
                if (_selectedTestPlanItem != null)
                {
                    _selectedTestPlanItem.PropertyChanged -= SelectedTestPlanItemPropertyChanged;
                }

                if (SetField(ref _selectedTestPlanItem, value))
                {
                    if (_selectedTestPlanItem != null)
                    {
                        _selectedTestPlanItem.PropertyChanged += SelectedTestPlanItemPropertyChanged;
                    }

                    OnPropertyChanged(nameof(SelectedTestPlanDetailText));
                }
                else if (_selectedTestPlanItem != null)
                {
                    _selectedTestPlanItem.PropertyChanged += SelectedTestPlanItemPropertyChanged;
                }
            }
        }

        public string SelectedTestPlanDetailText
        {
            get { return BuildSelectedTestPlanDetailText(); }
        }

        public bool HasTestPlanValidationIssues
        {
            get { return _hasTestPlanValidationIssues; }
            private set
            {
                if (SetField(ref _hasTestPlanValidationIssues, value))
                {
                    OnPropertyChanged(nameof(TestPlanValidationVisibility));
                }
            }
        }

        public Visibility TestPlanValidationVisibility
        {
            get { return HasTestPlanValidationIssues ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string RfpFlashScriptPath
        {
            get { return _rfpFlashScriptPath; }
            set { SetField(ref _rfpFlashScriptPath, value); }
        }

        public string TconFlashScriptPath
        {
            get { return _tconFlashScriptPath; }
            set { SetField(ref _tconFlashScriptPath, value); }
        }

        public string TconBinFilePath
        {
            get { return _tconBinFilePath; }
            set { SetField(ref _tconBinFilePath, value); }
        }

        public string TddiFlashScriptPath
        {
            get { return _tddiFlashScriptPath; }
            set { SetField(ref _tddiFlashScriptPath, value); }
        }

        public string TddiSerialPort
        {
            get { return _tddiSerialPort; }
            set { SetField(ref _tddiSerialPort, value); }
        }

        public string IoDaqCom
        {
            get { return _ioDaqCom; }
            set { SetField(ref _ioDaqCom, value); }
        }

        public string ScannerCom
        {
            get { return _scannerCom; }
            set { SetField(ref _scannerCom, value); }
        }

        public string OscilloscopeHost
        {
            get { return _oscilloscopeHost; }
            set { SetField(ref _oscilloscopeHost, value); }
        }

        public string FctStation
        {
            get { return _fctStation; }
            set { SetField(ref _fctStation, value); }
        }

        public bool SafetyEnabled
        {
            get { return _safetyEnabled; }
            set { SetField(ref _safetyEnabled, value); }
        }

        public int SafetyPollIntervalMs
        {
            get { return _safetyPollIntervalMs; }
            set { SetField(ref _safetyPollIntervalMs, value); }
        }

        public int EmergencyStopChannel
        {
            get { return _emergencyStopChannel; }
            set { SetField(ref _emergencyStopChannel, value); }
        }

        public bool EmergencyStopTriggeredValue
        {
            get { return _emergencyStopTriggeredValue; }
            set { SetField(ref _emergencyStopTriggeredValue, value); }
        }

        public int LightCurtainChannel
        {
            get { return _lightCurtainChannel; }
            set { SetField(ref _lightCurtainChannel, value); }
        }

        public bool LightCurtainTriggeredValue
        {
            get { return _lightCurtainTriggeredValue; }
            set { SetField(ref _lightCurtainTriggeredValue, value); }
        }

        public int FixtureDownOutputChannel
        {
            get { return _fixtureDownOutputChannel; }
            set { SetField(ref _fixtureDownOutputChannel, value); }
        }

        public int FixtureUpOutputChannel
        {
            get { return _fixtureUpOutputChannel; }
            set { SetField(ref _fixtureUpOutputChannel, value); }
        }

        public int FixtureUpDelayMs
        {
            get { return _fixtureUpDelayMs; }
            set { SetField(ref _fixtureUpDelayMs, value); }
        }

        public string CurrentTimeText
        {
            get { return _currentTimeText; }
            private set { SetField(ref _currentTimeText, value); }
        }

        public string RunElapsedSecondsText
        {
            get { return _runElapsedSecondsText; }
            private set { SetField(ref _runElapsedSecondsText, value); }
        }

        public void RefreshCurrentTime(DateTime now)
        {
            CurrentTimeText = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public int GoodCount
        {
            get { return _goodCount; }
            private set { SetField(ref _goodCount, value); }
        }

        public int FailCount
        {
            get { return _failCount; }
            private set { SetField(ref _failCount, value); }
        }

        public int ErrorCount
        {
            get { return _errorCount; }
            private set { SetField(ref _errorCount, value); }
        }

        public int TotalCount
        {
            get { return _totalCount; }
            private set { SetField(ref _totalCount, value); }
        }

        public string YieldRateText
        {
            get { return _yieldRateText; }
            private set { SetField(ref _yieldRateText, value); }
        }

        public double YieldRatePercent
        {
            get { return _yieldRatePercent; }
            private set { SetField(ref _yieldRatePercent, value); }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            private set
            {
                if (SetField(ref _isRunning, value))
                {
                    _startCommand.RaiseCanExecuteChanged();
                    _stopCommand.RaiseCanExecuteChanged();
                    _saveSettingsCommand.RaiseCanExecuteChanged();
                    _saveStationConfigCommand.RaiseCanExecuteChanged();
                    _reloadStationConfigCommand.RaiseCanExecuteChanged();
                    _saveTestPlanCommand.RaiseCanExecuteChanged();
                    _reloadTestPlanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<StepTreeNodeViewModel> StepTree { get; } = new ObservableCollection<StepTreeNodeViewModel>();

        public ObservableCollection<StepResultViewModel> Results { get; } = new ObservableCollection<StepResultViewModel>();

        public ObservableCollection<StepResultViewModel> FailureResults { get; } = new ObservableCollection<StepResultViewModel>();

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public ObservableCollection<SelfCheckItemViewModel> HardwareSelfCheckItems { get; } = new ObservableCollection<SelfCheckItemViewModel>();

        public ObservableCollection<string> PlaceholderItems { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> LanguageOptions { get; } = new ObservableCollection<string> { "中文", "English" };

        public ObservableCollection<string> ProductNameOptions { get; } = new ObservableCollection<string> { "K7000", "K7048", "K7049" };

        public ObservableCollection<string> MockScenarioOptions { get; } = new ObservableCollection<string>();

        public ObservableCollection<TestPlanItemEditorViewModel> TestPlanItems { get; } = new ObservableCollection<TestPlanItemEditorViewModel>();

        public ObservableCollection<string> TestPlanValidationIssues { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> TestItemKindOptions { get; } = new ObservableCollection<string>(Enum.GetNames(typeof(TestItemKind)));

        public string WindowTitleText { get { return "RFP Test Station"; } }

        public string TitleText { get { return T("自动化测试系统", "Automated Test System"); } }

        public string CurrentUserLabel { get { return T("当前用户", "Current User"); } }

        public string ProductNameLabel { get { return T("产品名称", "Product Name"); } }

        public string LanguageLabel { get { return T("当前语言", "Current Language"); } }

        public string TestPlanLabel { get { return T("测试序列", "Test Plan"); } }

        public string ConfigLabel { get { return T("测试配置", "Test Config"); } }

        public string ExecutionModeLabel { get { return T("运行模式", "Execution Mode"); } }

        public string ExecutionModeStatusText { get { return T("当前模式: ", "Mode: ") + ExecutionMode; } }

        public string MockScenarioLabel { get { return T("Mock 场景", "Mock Scenario"); } }

        public Visibility MockScenarioVisibility
        {
            get { return string.Equals(ExecutionMode, "Mock", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsHardwareMode
        {
            get { return string.Equals(ExecutionMode, "Hardware", StringComparison.OrdinalIgnoreCase); }
        }

        public string HardwareModeWarningText
        {
            get { return T("硬件模式：将真实控制设备、夹具和烧录脚本，请确认治具与产品已准备好。", "Hardware mode: real devices, fixture IO, and flash scripts will be controlled."); }
        }

        public Visibility HardwareModeWarningVisibility
        {
            get { return IsHardwareMode ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string TestPlanEditorPageTitleText { get { return T("测试计划维护", "Test Plan Editor"); } }

        public string StartupConfigTitleText { get { return T("启动配置", "Startup Settings"); } }

        public string TestPlanItemsTitleText { get { return T("测试项", "Test Items"); } }

        public string ReloadTestPlanButtonText { get { return T("重新加载计划", "Reload Plan"); } }

        public string SaveTestPlanButtonText { get { return T("保存测试计划", "Save Test Plan"); } }

        public string EditableTestPlanNameLabel { get { return T("计划名称", "Plan Name"); } }

        public string EditableTestPlanVersionLabel { get { return T("版本", "Version"); } }

        public string EditableTestPlanProductLabel { get { return T("产品", "Product"); } }

        public string SaveSettingsButtonText { get { return T("保存配置", "Save Settings"); } }

        public string SaveStationConfigButtonText { get { return T("保存站点配置", "Save Station Config"); } }

        public string ReloadStationConfigButtonText { get { return T("重新加载", "Reload"); } }

        public string SettingsPageTitleText { get { return T("站点配置", "Station Config"); } }

        public string CommonSettingsSectionText { get { return T("常用", "Common"); } }

        public string StartupSettingsSectionText { get { return T("启动", "Startup"); } }

        public string FlashingSettingsSectionText { get { return T("烧录", "Flashing"); } }

        public string InstrumentSettingsSectionText { get { return T("仪器", "Instruments"); } }

        public string FctSettingsSectionText { get { return T("FCT", "FCT"); } }

        public string SafetySettingsSectionText { get { return T("安全", "Safety"); } }

        public string AdvancedSettingsSectionText { get { return T("高级", "Advanced"); } }

        public string SaveAllStationConfigButtonText { get { return T("保存全部配置", "Save All Config"); } }

        public string FlashConfigTitleText { get { return T("烧录配置", "Flash Config"); } }

        public string InstrumentConfigTitleText { get { return T("仪器配置", "Instrument Config"); } }

        public string FctConfigTitleText { get { return T("FCT 配置", "FCT Config"); } }

        public string SafetyConfigTitleText { get { return T("安全监控", "Safety Monitor"); } }

        public string RfpFlashScriptLabel { get { return T("RFP 脚本", "RFP Script"); } }

        public string TconFlashScriptLabel { get { return T("TCON 脚本", "TCON Script"); } }

        public string TconBinFileLabel { get { return T("TCON 固件", "TCON Firmware"); } }

        public string TddiFlashScriptLabel { get { return T("TDDI 脚本", "TDDI Script"); } }

        public string TddiSerialPortLabel { get { return T("TDDI 串口", "TDDI Serial Port"); } }

        public string IoDaqComLabel { get { return T("IO DAQ 串口", "IO DAQ COM"); } }

        public string ScannerComLabel { get { return T("扫码枪串口", "Scanner COM"); } }

        public string OscilloscopeHostLabel { get { return T("示波器地址", "Oscilloscope Host"); } }

        public string FctStationLabel { get { return T("工站名称", "Station Name"); } }

        public string SafetyEnabledLabel { get { return T("启用安全监控", "Enable Safety Monitor"); } }

        public string SafetyPollIntervalLabel { get { return T("轮询周期(ms)", "Poll Interval (ms)"); } }

        public string EmergencyStopChannelLabel { get { return T("急停输入 X", "E-Stop Input X"); } }

        public string EmergencyStopTriggeredValueLabel { get { return T("急停触发值", "E-Stop Trigger Value"); } }

        public string LightCurtainChannelLabel { get { return T("光栅输入 X", "Light Curtain Input X"); } }

        public string LightCurtainTriggeredValueLabel { get { return T("光栅触发值", "Light Curtain Trigger Value"); } }

        public string FixtureDownOutputChannelLabel { get { return T("下压输出 Y", "Down Output Y"); } }

        public string FixtureUpOutputChannelLabel { get { return T("上升输出 Y", "Up Output Y"); } }

        public string FixtureUpDelayLabel { get { return T("上升延时(ms)", "Up Delay (ms)"); } }

        public string OperationsLabel { get { return T("操作", "Operation"); } }

        public string LogoutButtonText { get { return T("登  出", "Logout"); } }

        public string StartButtonText { get { return T("开  始", "Start"); } }

        public string PauseButtonText { get { return T("暂  停", "Pause"); } }

        public string EndButtonText { get { return T("结  束", "End"); } }

        public string ProgressLabel { get { return T("测试进度", "Test Progress"); } }

        public string ElapsedTimeLabel { get { return T("当前测试时间(s)", "Current Test Time (s)"); } }

        public string SerialNumberLabel { get { return T("产品序列号读取", "Product Serial Number"); } }

        public string StatisticsLabel { get { return T("统计", "Statistics"); } }

        public string GoodLabel { get { return T("良  品", "Good"); } }

        public string FailLabel { get { return T("不合格品", "Fail"); } }

        public string ErrorLabel { get { return T("错  误", "Error"); } }

        public string TotalLabel { get { return T("总  计", "Total"); } }

        public string YieldLabel { get { return T("良率", "Yield"); } }

        public string ResetStatisticsText { get { return T("重新统计", "Reset Statistics"); } }

        public string FooterSloganText { get { return T("高可靠性 . 高精准 . 全数字化", "High Reliability . High Precision . Fully Digital"); } }

        public string StepHeaderText { get { return T("STEP", "STEP"); } }

        public string EnabledHeaderText { get { return T("启用", "Enabled"); } }

        public string RequiredHeaderText { get { return T("必测", "Required"); } }

        public string StopOnFailureHeaderText { get { return T("失败中断", "Stop On Failure"); } }

        public string TimeoutHeaderText { get { return T("超时(s)", "Timeout (s)"); } }

        public string KindHeaderText { get { return T("类型", "Kind"); } }

        public string ScriptHeaderText { get { return T("脚本", "Script"); } }

        public string AdapterHeaderText { get { return T("适配器", "Adapter"); } }

        public string OperationHeaderText { get { return T("操作", "Operation"); } }

        public string NoteHeaderText { get { return T("备注", "Note"); } }

        public string SummaryHeaderText { get { return T("摘要", "Summary"); } }

        public string DescriptionHeaderText { get { return T("DESCRIPTION", "DESCRIPTION"); } }

        public string StatusHeaderText { get { return T("STATUS", "STATUS"); } }

        public string MeasurementHeaderText { get { return T("测量值", "Measurement"); } }

        public string ExpectedValueHeaderText { get { return T("期望值", "Expected"); } }

        public string TargetHeaderText { get { return T("对象/通道", "Target"); } }

        public string UnitsHeaderText { get { return T("单位", "Units"); } }

        public string LowLimitHeaderText { get { return T("下限", "Low Limit"); } }

        public string HighLimitHeaderText { get { return T("上限", "High Limit"); } }

        public string ComparisonTypeHeaderText { get { return T("比较类型", "Comparison Type"); } }

        public ICommand StartCommand
        {
            get { return _startCommand; }
        }

        public ICommand StopCommand
        {
            get { return _stopCommand; }
        }

        public ICommand ResetCommand
        {
            get { return _resetCommand; }
        }

        public ICommand ResetStatisticsCommand
        {
            get { return _resetStatisticsCommand; }
        }

        public ICommand SaveSettingsCommand
        {
            get { return _saveSettingsCommand; }
        }

        public ICommand SaveStationConfigCommand
        {
            get { return _saveStationConfigCommand; }
        }

        public ICommand ReloadStationConfigCommand
        {
            get { return _reloadStationConfigCommand; }
        }

        public ICommand SaveTestPlanCommand
        {
            get { return _saveTestPlanCommand; }
        }

        public ICommand ReloadTestPlanCommand
        {
            get { return _reloadTestPlanCommand; }
        }

        public ICommand ShowRunPageCommand
        {
            get { return _showRunPageCommand; }
        }

        public ICommand ShowTestPlanPageCommand
        {
            get { return _showTestPlanPageCommand; }
        }

        public ICommand ShowSettingsPageCommand
        {
            get { return _showSettingsPageCommand; }
        }

        public ICommand SelectSettingsSectionCommand
        {
            get { return _selectSettingsSectionCommand; }
        }

        public async Task StartRunAsync()
        {
            Reset();
            RunAutomaticHardwareSelfCheck();
            IsRunning = true;
            OverallStatus = "Running";
            OverallStatusReason = string.Empty;
            _runCancellation = new CancellationTokenSource();
            CancellationTokenSource? safetyMonitorCancellation = null;
            Task? safetyTask = null;
            StationSafetySupervisor? safetySupervisor = null;

            try
            {
                var serialNumberError = SerialNumberValidator.Validate(SerialNumber, true, string.Empty);
                if (serialNumberError != null)
                {
                    OverallStatus = "Error";
                    OverallStatusReason = serialNumberError;
                    AddRunBlockerFailure("Run Start", serialNumberError);
                    Logs.Add(serialNumberError);
                    return;
                }

                var testPlanPath = ResolveConfiguredPath(TestPlanPath);
                var configJsonPath = ResolveConfiguredPath(ConfigJsonPath);
                var preflightResults = new StationPreflightChecker().Check(testPlanPath, configJsonPath);
                foreach (var check in preflightResults)
                {
                    Logs.Add("Preflight " + (check.Passed ? "PASS" : "FAIL") + " [" + check.Name + "]: " + check.Message);
                }

                if (preflightResults.Any(x => !x.Passed))
                {
                    var failedPreflightResults = preflightResults.Where(x => !x.Passed).ToList();
                    var firstFailure = failedPreflightResults.First();
                    OverallStatus = "Error";
                    OverallStatusReason = firstFailure.Name + ": " + firstFailure.Message;
                    foreach (var failedPreflight in failedPreflightResults)
                    {
                        AddRunBlockerFailure("Preflight: " + failedPreflight.Name, failedPreflight.Message);
                    }

                    Logs.Add("Preflight failed. Run blocked.");
                    return;
                }

                Logs.Add("Loading test plan: " + testPlanPath);
                new ConfigRepository(configJsonPath).Load();
                ConfigName = Path.GetFileName(configJsonPath);
                Logs.Add("Loaded config: " + configJsonPath);

                var testPlan = TestPlanRepository.Load(testPlanPath);
                TestPlanName = testPlan.Name;
                Logs.Add("Test plan: " + testPlan.Name + " v" + testPlan.Version);
                var testItems = TestPlanWorkflowFactory.CreateItems(testPlan).ToList();
                ApplySelectedMockScenario(testItems);
                StepTree.Clear();
                foreach (var item in testItems)
                {
                    StepTree.Add(StepTreeNodeViewModel.FromTestItem(item));
                }

                InitializeResultRows(testItems);
                ProgressText = "0 / " + testItems.Count;
                ProgressPercent = 0.0;
                var adapterRegistry = CreateAdapterRegistry(_repoRoot);
                var stationIo = adapterRegistry.ModbusIo as IStationIoController;
                if (SafetyEnabled
                    && stationIo != null
                    && string.Equals(ExecutionMode, "Hardware", StringComparison.OrdinalIgnoreCase))
                {
                    safetyMonitorCancellation = new CancellationTokenSource();
                    safetySupervisor = new StationSafetySupervisor(stationIo, CreateSafetyOptions());
                    safetyTask = safetySupervisor.RunAsync(_runCancellation, safetyMonitorCancellation.Token);
                    Logs.Add("Safety monitor started.");
                }

                var testPlanExecutor = new TestPlanItemExecutor(adapterRegistry);
                var workflow = new FunctionalTestWorkflow(testItems, testPlanExecutor.ExecuteAsync)
                {
                    ItemStarted = MarkResultRunning,
                    ItemCompleted = result => MarkResultCompleted(result, testItems.Count)
                };
                var startedAt = DateTimeOffset.Now;
                var workflowResult = await workflow.RunAsync(
                    new WorkflowRunContext
                    {
                        SerialNumber = SerialNumber,
                        Mode = ExecutionMode
                    },
                    _runCancellation.Token).ConfigureAwait(true);
                var finishedAt = DateTimeOffset.Now;
                RunElapsedSecondsText = (finishedAt - startedAt).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture);

                var resultIndex = 0;
                foreach (var result in workflowResult.Results)
                {
                    resultIndex++;
                    Logs.Add(FormatStepLog(resultIndex, testItems.Count, result));
                }

                OverallStatus = safetySupervisor != null && safetySupervisor.LastTrigger != null
                    ? "SafetyStop"
                    : workflowResult.Status == StepStatus.Stopped
                    ? "Stopped"
                    : workflowResult.Passed ? "Pass" : "Fail";
                OverallStatusReason = BuildWorkflowStatusReason(workflowResult);
                UpdateProductionStatistics(OverallStatus);
                WriteReports(
                    _stationPaths.ReportsDirectory,
                    startedAt,
                    finishedAt,
                    testPlan.Name,
                    testPlanPath,
                    configJsonPath,
                    testItems,
                    workflowResult.Results);
                if (safetySupervisor != null && safetySupervisor.LastTrigger != null)
                {
                    Logs.Add("Safety triggered: " + safetySupervisor.LastTrigger.Message);
                }

                Logs.Add("Run finished: " + OverallStatus);
            }
            catch (OperationCanceledException)
            {
                OverallStatus = safetySupervisor != null && safetySupervisor.LastTrigger != null ? "SafetyStop" : "Stopped";
                OverallStatusReason = safetySupervisor != null && safetySupervisor.LastTrigger != null
                    ? safetySupervisor.LastTrigger.Message
                    : T("用户停止运行", "Run stopped by operator");
                UpdateProductionStatistics(OverallStatus);
                Logs.Add(OverallStatus == "SafetyStop" ? "Run stopped by safety monitor." : "Run stopped.");
            }
            catch (Exception ex)
            {
                OverallStatus = "Error";
                OverallStatusReason = ex.Message;
                if (FailureResults.Count == 0)
                {
                    AddRunBlockerFailure("Runtime Error", ex.Message);
                }

                UpdateProductionStatistics(OverallStatus);
                Logs.Add(ex.Message);
            }
            finally
            {
                safetyMonitorCancellation?.Cancel();
                if (safetyTask != null)
                {
                    try
                    {
                        await safetyTask.ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                safetyMonitorCancellation?.Dispose();
                IsRunning = false;
                _runCancellation?.Dispose();
                _runCancellation = null;
            }
        }

        private void StartRun()
        {
            _ = StartRunAsync();
        }

        private void StopRun()
        {
            _runCancellation?.Cancel();
        }

        private void RunAutomaticHardwareSelfCheck()
        {
            HardwareSelfCheckItems.Clear();

            var hardwareMode = string.Equals(ExecutionMode, "Hardware", StringComparison.OrdinalIgnoreCase);
            AddSelfCheck(
                ExecutionModeLabel,
                hardwareMode ? "警告" : "通过",
                hardwareMode
                    ? T("当前为 Hardware，会真实控制外部设备。", "Hardware mode will control real external devices.")
                    : T("当前为 Mock，不会真实控制硬件。", "Mock mode does not control real hardware."));

            try
            {
                var testPlanPath = ResolveConfiguredPath(TestPlanPath);
                var configJsonPath = ResolveConfiguredPath(ConfigJsonPath);
                foreach (var check in new StationPreflightChecker().Check(testPlanPath, configJsonPath))
                {
                    AddSelfCheck(check.Name, check.Passed ? "通过" : "失败", check.Message);
                }

                var config = new ConfigRepository(configJsonPath).Load();
                AddConfiguredValueCheck("TDDI 串口", config.GetValue("Burn3.Params.Serial.PortName"));
                AddConfiguredValueCheck("IO DAQ 串口", config.GetValue("Instruments[0].Com"));
                AddConfiguredValueCheck("扫码枪串口", config.GetValue("Instruments[1].Com"));
                AddConfiguredValueCheck("示波器地址", config.GetValue("Instruments[2].Host"));
            }
            catch (Exception ex)
            {
                AddSelfCheck(T("自检异常", "Self-check error"), "失败", ex.Message);
            }

            Logs.Add("Hardware self-check completed: " + HardwareSelfCheckItems.Count.ToString(CultureInfo.InvariantCulture) + " items.");
        }

        private void AddConfiguredValueCheck(string name, string value)
        {
            AddSelfCheck(
                name,
                string.IsNullOrWhiteSpace(value) ? "失败" : "通过",
                string.IsNullOrWhiteSpace(value) ? T("未配置", "Not configured") : value);
        }

        private void AddSelfCheck(string name, string status, string message)
        {
            HardwareSelfCheckItems.Add(new SelfCheckItemViewModel
            {
                Name = name,
                Status = status,
                Message = message
            });
            Logs.Add("Self-check " + status + " [" + name + "]: " + message);
        }

        private void ApplySettings(AppSettings settings)
        {
            CurrentUser = settings.CurrentUser;
            ProductName = settings.ProductName;
            SelectedLanguage = settings.SelectedLanguage;
            ExecutionMode = settings.ExecutionMode;
            SelectedMockScenarioName = settings.MockScenarioName;
            TestPlanPath = settings.TestPlanPath;
            ConfigJsonPath = settings.ConfigJsonPath;
            RefreshConfiguredFileNames();
        }

        private void SaveSettings()
        {
            _settingsRepository.Save(new AppSettings
            {
                CurrentUser = CurrentUser,
                ProductName = ProductName,
                SelectedLanguage = SelectedLanguage,
                ExecutionMode = ExecutionMode,
                TestPlanPath = TestPlanPath,
                ConfigJsonPath = ConfigJsonPath,
                MockScenarioName = SelectedMockScenarioName
            });

            SettingsStatusText = T("配置已保存", "Settings saved");
            Logs.Add("Saved startup settings: " + _stationPaths.AppSettingsPath);
            RunAutomaticHardwareSelfCheck();
        }

        private string NormalizeProductName(string? productName)
        {
            return ProductNameOptions.Contains(productName ?? string.Empty)
                ? productName!
                : "K7000";
        }

        private string NormalizeMockScenarioName(string? scenarioName)
        {
            var name = string.IsNullOrWhiteSpace(scenarioName) ? NoMockScenarioName : scenarioName!.Trim();
            if (!MockScenarioOptions.Contains(name))
            {
                MockScenarioOptions.Add(name);
            }

            return name;
        }

        private void LoadMockScenarioOptions()
        {
            MockScenarioOptions.Clear();
            MockScenarioOptions.Add(NoMockScenarioName);
            foreach (var scenario in new MockScenarioRepository(_stationPaths.MockScenariosDirectory).LoadAvailable())
            {
                if (!MockScenarioOptions.Contains(scenario.Name))
                {
                    MockScenarioOptions.Add(scenario.Name);
                }
            }
        }

        private void ApplySelectedMockScenario(IList<TestItem> testItems)
        {
            if (!string.Equals(ExecutionMode, "Mock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(SelectedMockScenarioName, NoMockScenarioName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var repository = new MockScenarioRepository(_stationPaths.MockScenariosDirectory);
            var scenario = repository.LoadAvailable()
                .FirstOrDefault(x => string.Equals(x.Name, SelectedMockScenarioName, StringComparison.OrdinalIgnoreCase));
            if (scenario == null)
            {
                Logs.Add("Mock scenario not found: " + SelectedMockScenarioName);
                return;
            }

            var appliedCount = repository.Apply(scenario.Path, testItems);
            Logs.Add("Mock scenario applied: " + SelectedMockScenarioName + "; items=" + appliedCount.ToString(CultureInfo.InvariantCulture));
        }

        private void LoadTestPlanEditor()
        {
            try
            {
                var testPlanPath = ResolveConfiguredPath(TestPlanPath);
                var plan = TestPlanRepository.Load(testPlanPath);
                EditableTestPlanName = plan.Name;
                EditableTestPlanVersion = plan.Version;
                EditableTestPlanProduct = plan.Product;
                TestPlanItems.Clear();
                foreach (var item in plan.Items)
                {
                    TestPlanItems.Add(TestPlanItemEditorViewModel.FromDefinition(item));
                }

                SelectedTestPlanItem = TestPlanItems.FirstOrDefault();
                TestPlanValidationIssues.Clear();
                HasTestPlanValidationIssues = false;
                TestPlanEditorStatusText = T("测试计划已加载", "Test plan loaded");
                if (!_isInitializing)
                {
                    RunAutomaticHardwareSelfCheck();
                }
            }
            catch (Exception ex)
            {
                TestPlanEditorStatusText = ex.Message;
                Logs.Add(ex.Message);
            }
        }

        private void SaveTestPlan()
        {
            try
            {
                var testPlanPath = ResolveConfiguredPath(TestPlanPath);
                if (!ValidateTestPlanEditor(testPlanPath))
                {
                    TestPlanEditorStatusText = "Test plan validation failed: " + TestPlanValidationIssues.Count.ToString(CultureInfo.InvariantCulture) + " issue(s)";
                    Logs.Add(TestPlanEditorStatusText);
                    return;
                }

                var plan = new TestPlanDefinition
                {
                    Name = EditableTestPlanName,
                    Version = EditableTestPlanVersion,
                    Product = EditableTestPlanProduct
                };
                foreach (var item in TestPlanItems)
                {
                    plan.Items.Add(item.ToDefinition());
                }

                TestPlanRepository.Save(plan, testPlanPath);
                TestPlanName = plan.Name;
                TestPlanValidationIssues.Clear();
                HasTestPlanValidationIssues = false;
                TestPlanEditorStatusText = T("测试计划已保存", "Test plan saved");
                Logs.Add("Saved test plan: " + testPlanPath);
                RunAutomaticHardwareSelfCheck();
            }
            catch (Exception ex)
            {
                TestPlanEditorStatusText = ex.Message;
                Logs.Add(ex.Message);
            }
        }

        private bool ValidateTestPlanEditor(string testPlanPath)
        {
            TestPlanValidationIssues.Clear();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in TestPlanItems)
            {
                var itemId = string.IsNullOrWhiteSpace(item.Id) ? "<missing id>" : item.Id;
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    TestPlanValidationIssues.Add("Item ID is required.");
                }
                else if (!ids.Add(item.Id))
                {
                    TestPlanValidationIssues.Add("Duplicate item ID: " + item.Id);
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    TestPlanValidationIssues.Add(itemId + ": name is required.");
                }

                if (!Enum.TryParse(item.KindText, true, out TestItemKind kind))
                {
                    TestPlanValidationIssues.Add(itemId + ": kind is invalid: " + item.KindText);
                }

                if (item.TimeoutSeconds <= 0)
                {
                    TestPlanValidationIssues.Add(itemId + ": timeoutSeconds must be greater than zero.");
                }

                AddLimitValidationIssue(item);
                AddScriptValidationIssue(item, testPlanPath);
                AddParameterValidationIssues(item, itemId);
            }

            HasTestPlanValidationIssues = TestPlanValidationIssues.Count > 0;
            return !HasTestPlanValidationIssues;
        }

        private void AddLimitValidationIssue(TestPlanItemEditorViewModel item)
        {
            if (double.TryParse(item.LowLimit, NumberStyles.Float, CultureInfo.InvariantCulture, out var low)
                && double.TryParse(item.HighLimit, NumberStyles.Float, CultureInfo.InvariantCulture, out var high)
                && low > high)
            {
                TestPlanValidationIssues.Add(item.Id + ": low limit must not be greater than high limit.");
            }
        }

        private void AddScriptValidationIssue(TestPlanItemEditorViewModel item, string testPlanPath)
        {
            if (!string.Equals(item.KindText, TestItemKind.Flash.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(item.Script))
            {
                return;
            }

            var scriptPath = ResolveEditorAssetPath(item.Script, testPlanPath);
            if (!File.Exists(scriptPath))
            {
                TestPlanValidationIssues.Add(item.Id + ": Script file does not exist: " + item.Script);
            }
        }

        private void AddParameterValidationIssues(TestPlanItemEditorViewModel item, string itemId)
        {
            JObject parameters;
            try
            {
                parameters = string.IsNullOrWhiteSpace(item.ParametersJson)
                    ? new JObject()
                    : JObject.Parse(item.ParametersJson);
            }
            catch (JsonException ex)
            {
                TestPlanValidationIssues.Add(itemId + ": parameters JSON is invalid: " + ex.Message);
                return;
            }

            var template = parameters["template"] == null ? string.Empty : parameters["template"]!.ToString();
            if (!string.Equals(template, "I2cFunctionalGroup", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var children = parameters["items"] as JArray;
            if (children == null || children.Count == 0)
            {
                TestPlanValidationIssues.Add(itemId + ": Functional group has no child items.");
                return;
            }

            for (var index = 0; index < children.Count; index++)
            {
                var child = children[index] as JObject;
                if (child == null)
                {
                    TestPlanValidationIssues.Add(itemId + ": child " + index.ToString(CultureInfo.InvariantCulture) + " must be an object.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ReadJsonString(child, "name")))
                {
                    TestPlanValidationIssues.Add(itemId + ": child " + index.ToString(CultureInfo.InvariantCulture) + " name is required.");
                }

                if (string.IsNullOrWhiteSpace(ReadJsonString(child, "template")))
                {
                    TestPlanValidationIssues.Add(itemId + ": child " + index.ToString(CultureInfo.InvariantCulture) + " template is required.");
                }
            }
        }

        private static string ReadJsonString(JObject value, string name)
        {
            var token = value[name];
            return token == null ? string.Empty : token.ToString();
        }

        private string ResolveEditorAssetPath(string path, string testPlanPath)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            var repoRelative = Path.Combine(_repoRoot, path);
            if (File.Exists(repoRelative))
            {
                return repoRelative;
            }

            var planDirectory = Path.GetDirectoryName(testPlanPath) ?? _repoRoot;
            return Path.Combine(planDirectory, path);
        }

        private string BuildSelectedTestPlanDetailText()
        {
            var item = SelectedTestPlanItem;
            if (item == null)
            {
                return T("选择一个测试项查看详情", "Select a test item to view details");
            }

            var builder = new StringBuilder();
            builder.AppendLine(item.Name);
            builder.AppendLine("ID: " + item.Id);
            builder.AppendLine("Kind: " + item.KindText);
            builder.AppendLine("Timeout: " + item.TimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s");
            if (!string.IsNullOrWhiteSpace(item.SummaryText))
            {
                builder.AppendLine("Summary: " + item.SummaryText);
            }

            AddDetailLine(builder, "Script", item.Script);
            AddDetailLine(builder, "Adapter", item.Adapter);
            AddDetailLine(builder, "Operation", item.Operation);
            if (!string.IsNullOrWhiteSpace(item.LowLimit) || !string.IsNullOrWhiteSpace(item.HighLimit))
            {
                builder.AppendLine("Limit: " + item.LowLimit + " .. " + item.HighLimit + (string.IsNullOrWhiteSpace(item.Unit) ? string.Empty : " " + item.Unit));
            }

            if (!string.IsNullOrWhiteSpace(item.GroupChildrenText))
            {
                builder.AppendLine();
                builder.AppendLine("Children:");
                builder.AppendLine(item.GroupChildrenText);
            }

            builder.AppendLine();
            builder.AppendLine("Parameters:");
            builder.AppendLine(FormatParametersForDisplay(item.ParametersJson));
            return builder.ToString().TrimEnd();
        }

        private static string FormatParametersForDisplay(string parametersJson)
        {
            if (string.IsNullOrWhiteSpace(parametersJson))
            {
                return "{}";
            }

            try
            {
                return JToken.Parse(parametersJson).ToString(Formatting.Indented);
            }
            catch (JsonException)
            {
                return parametersJson;
            }
        }

        private static void AddDetailLine(StringBuilder builder, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.AppendLine(label + ": " + value);
            }
        }

        private void SelectedTestPlanItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TestPlanItemEditorViewModel.Name)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.KindText)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.TimeoutSeconds)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.Script)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.Adapter)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.Operation)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.LowLimit)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.HighLimit)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.Unit)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.ParametersJson)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.SummaryText)
                || e.PropertyName == nameof(TestPlanItemEditorViewModel.GroupChildrenText))
            {
                OnPropertyChanged(nameof(SelectedTestPlanDetailText));
            }
        }

        private void LoadStationConfigEditor()
        {
            try
            {
                var config = new ConfigRepository(ResolveConfiguredPath(ConfigJsonPath)).Load();
                RfpFlashScriptPath = config.GetValue("Burn1.Params.FilePath");
                TconFlashScriptPath = config.GetValue("Burn2.Params.FilePath");
                TconBinFilePath = config.GetValue("Burn2.Params.BinFilePath");
                TddiFlashScriptPath = config.GetValue("Burn3.Params.FilePath");
                TddiSerialPort = config.GetValue("Burn3.Params.Serial.PortName");
                IoDaqCom = config.GetValue("Instruments[0].Com");
                ScannerCom = config.GetValue("Instruments[1].Com");
                OscilloscopeHost = config.GetValue("Instruments[2].Host");
                FctStation = config.GetValue("FctTest.Global.Station");
                SafetyEnabled = ReadBool(config, "Safety.Enabled", true);
                SafetyPollIntervalMs = ReadInt(config, "Safety.PollIntervalMs", 50);
                EmergencyStopChannel = ReadInt(config, "Safety.Inputs.EmergencyStop.Channel", 4);
                EmergencyStopTriggeredValue = ReadBool(config, "Safety.Inputs.EmergencyStop.TriggeredValue", true);
                LightCurtainChannel = ReadInt(config, "Safety.Inputs.LightCurtain.Channel", 5);
                LightCurtainTriggeredValue = ReadBool(config, "Safety.Inputs.LightCurtain.TriggeredValue", false);
                FixtureDownOutputChannel = ReadInt(config, "Safety.ReleaseFixture.DownOutputChannel", 2);
                FixtureUpOutputChannel = ReadInt(config, "Safety.ReleaseFixture.UpOutputChannel", 1);
                FixtureUpDelayMs = ReadInt(config, "Safety.ReleaseFixture.UpDelayMs", 1000);
                HasStationConfigValidationError = false;
                StationConfigValidationText = string.Empty;
                StationConfigStatusText = T("站点配置已加载", "Station config loaded");
                if (!_isInitializing)
                {
                    RunAutomaticHardwareSelfCheck();
                }
            }
            catch (Exception ex)
            {
                StationConfigStatusText = ex.Message;
                Logs.Add(ex.Message);
            }
        }

        private void SaveStationConfig()
        {
            try
            {
                string validationMessage;
                if (!ValidateStationConfigEditor(out validationMessage))
                {
                    HasStationConfigValidationError = true;
                    StationConfigValidationText = validationMessage;
                    StationConfigStatusText = validationMessage;
                    Logs.Add("Station config validation failed: " + validationMessage);
                    return;
                }

                var configPath = ResolveConfiguredPath(ConfigJsonPath);
                var repository = new ConfigRepository(configPath);
                var config = repository.Load();
                config.SetValue("Burn1.Params.FilePath", RfpFlashScriptPath);
                config.SetValue("Burn2.Params.FilePath", TconFlashScriptPath);
                config.SetValue("Burn2.Params.BinFilePath", TconBinFilePath);
                config.SetValue("Burn3.Params.FilePath", TddiFlashScriptPath);
                config.SetValue("Burn3.Params.Serial.PortName", TddiSerialPort);
                config.SetValue("Instruments[0].Com", IoDaqCom);
                config.SetValue("Instruments[1].Com", ScannerCom);
                config.SetValue("Instruments[2].Host", OscilloscopeHost);
                config.SetValue("FctTest.Global.Station", FctStation);
                config.SetValue("Safety.Enabled", SafetyEnabled);
                config.SetValue("Safety.PollIntervalMs", SafetyPollIntervalMs);
                config.SetValue("Safety.Inputs.EmergencyStop.Channel", EmergencyStopChannel);
                config.SetValue("Safety.Inputs.EmergencyStop.TriggeredValue", EmergencyStopTriggeredValue);
                config.SetValue("Safety.Inputs.LightCurtain.Channel", LightCurtainChannel);
                config.SetValue("Safety.Inputs.LightCurtain.TriggeredValue", LightCurtainTriggeredValue);
                config.SetValue("Safety.ReleaseFixture.DownOutputChannel", FixtureDownOutputChannel);
                config.SetValue("Safety.ReleaseFixture.DownSafeValue", false);
                config.SetValue("Safety.ReleaseFixture.UpOutputChannel", FixtureUpOutputChannel);
                config.SetValue("Safety.ReleaseFixture.UpPreValue", false);
                config.SetValue("Safety.ReleaseFixture.UpDelayMs", FixtureUpDelayMs);
                config.SetValue("Safety.ReleaseFixture.UpSafeValue", true);
                config.SetValue("Safety.OnTrigger.CancelRun", true);
                config.SetValue("Safety.OnTrigger.KillExternalProcessTree", true);
                config.SetValue("Safety.OnTrigger.LatchUntilManualReset", true);
                repository.Save(config);
                HasStationConfigValidationError = false;
                StationConfigValidationText = string.Empty;
                StationConfigStatusText = T("站点配置已保存", "Station config saved");
                Logs.Add("Saved station config: " + configPath);
                RunAutomaticHardwareSelfCheck();
            }
            catch (Exception ex)
            {
                StationConfigStatusText = ex.Message;
                Logs.Add(ex.Message);
            }
        }

        private bool ValidateStationConfigEditor(out string message)
        {
            if (string.IsNullOrWhiteSpace(RfpFlashScriptPath))
            {
                message = T("RFP 脚本路径不能为空", "RFP script path is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TconFlashScriptPath))
            {
                message = T("TCON 脚本路径不能为空", "TCON script path is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TconBinFilePath))
            {
                message = T("TCON 固件路径不能为空", "TCON firmware path is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TddiFlashScriptPath))
            {
                message = T("TDDI 脚本路径不能为空", "TDDI script path is required");
                return false;
            }

            if (!IsComPort(TddiSerialPort))
            {
                message = T("TDDI 串口必须是 COM 加数字", "TDDI serial port must be COM plus a number");
                return false;
            }

            if (!IsComPort(IoDaqCom))
            {
                message = T("IO DAQ 串口必须是 COM 加数字", "IO DAQ COM must be COM plus a number");
                return false;
            }

            if (!IsComPort(ScannerCom))
            {
                message = T("扫码枪串口必须是 COM 加数字", "Scanner COM must be COM plus a number");
                return false;
            }

            if (!IsValidHost(OscilloscopeHost))
            {
                message = T("示波器地址格式不正确", "Oscilloscope host format is invalid");
                return false;
            }

            if (SafetyPollIntervalMs < 0)
            {
                message = T("安全轮询周期不能小于 0", "Safety poll interval must be greater than or equal to 0");
                return false;
            }

            if (EmergencyStopChannel < 0 || LightCurtainChannel < 0 || FixtureDownOutputChannel < 0 || FixtureUpOutputChannel < 0)
            {
                message = T("IO 通道不能小于 0", "IO channels must be greater than or equal to 0");
                return false;
            }

            if (FixtureUpDelayMs < 0)
            {
                message = T("夹具上升延时不能小于 0", "Fixture up delay must be greater than or equal to 0");
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool IsComPort(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && Regex.IsMatch(value.Trim(), @"^COM[0-9]+$", RegexOptions.IgnoreCase);
        }

        private static bool IsValidHost(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Trim().Split('.');
            if (parts.Length != 4)
            {
                return true;
            }

            foreach (var part in parts)
            {
                int octet;
                if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out octet)
                    || octet < 0
                    || octet > 255)
                {
                    return false;
                }
            }

            return true;
        }

        private string ResolveConfiguredPath(string path)
        {
            return _settingsRepository.ResolvePath(path);
        }

        private bool IsSettingsSection(string section)
        {
            return string.Equals(SelectedSettingsSection, section, StringComparison.OrdinalIgnoreCase);
        }

        private Visibility SectionVisibility(string section)
        {
            return IsSettingsSection(section) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RaiseSettingsSectionPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsCommonSettingsSection));
            OnPropertyChanged(nameof(IsStartupSettingsSection));
            OnPropertyChanged(nameof(IsFlashingSettingsSection));
            OnPropertyChanged(nameof(IsInstrumentSettingsSection));
            OnPropertyChanged(nameof(IsFctSettingsSection));
            OnPropertyChanged(nameof(IsSafetySettingsSection));
            OnPropertyChanged(nameof(IsAdvancedSettingsSection));
            OnPropertyChanged(nameof(CommonSettingsSectionVisibility));
            OnPropertyChanged(nameof(StartupSettingsSectionVisibility));
            OnPropertyChanged(nameof(FlashingSettingsSectionVisibility));
            OnPropertyChanged(nameof(InstrumentSettingsSectionVisibility));
            OnPropertyChanged(nameof(FctSettingsSectionVisibility));
            OnPropertyChanged(nameof(SafetySettingsSectionVisibility));
            OnPropertyChanged(nameof(AdvancedSettingsSectionVisibility));
        }

        private SafetyOptions CreateSafetyOptions()
        {
            return new SafetyOptions
            {
                Enabled = SafetyEnabled,
                PollIntervalMs = SafetyPollIntervalMs,
                EmergencyStop = new SafetyInputOptions
                {
                    Channel = EmergencyStopChannel,
                    TriggeredValue = EmergencyStopTriggeredValue
                },
                LightCurtain = new SafetyInputOptions
                {
                    Channel = LightCurtainChannel,
                    TriggeredValue = LightCurtainTriggeredValue
                },
                ReleaseFixture = new ReleaseFixtureOptions
                {
                    DownOutputChannel = FixtureDownOutputChannel,
                    DownSafeValue = false,
                    UpOutputChannel = FixtureUpOutputChannel,
                    UpPreValue = false,
                    UpDelayMs = FixtureUpDelayMs,
                    UpSafeValue = true
                },
                OnTrigger = new SafetyTriggerActionOptions
                {
                    CancelRun = true,
                    KillExternalProcessTree = true,
                    LatchUntilManualReset = true
                }
            };
        }

        private static bool ReadBool(StationConfig config, string path, bool defaultValue)
        {
            var value = config.GetValue(path);
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static int ReadInt(StationConfig config, string path, int defaultValue)
        {
            var value = config.GetValue(path);
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
        }

        private void RefreshConfiguredFileNames()
        {
            ConfigName = Path.GetFileName(ResolveConfiguredPath(ConfigJsonPath));
            var testPlanPath = ResolveConfiguredPath(TestPlanPath);
            if (!File.Exists(testPlanPath))
            {
                TestPlanName = Path.GetFileName(testPlanPath);
                return;
            }

            try
            {
                TestPlanName = TestPlanRepository.Load(testPlanPath).Name;
            }
            catch
            {
                TestPlanName = Path.GetFileName(testPlanPath);
            }
        }

        private void Reset()
        {
            Results.Clear();
            FailureResults.Clear();
            Logs.Clear();
            StepTree.Clear();
            OverallStatus = "Idle";
            OverallStatusReason = string.Empty;
            CurrentStep = "-";
            ProgressText = "0 / 0";
            ProgressPercent = 0.0;
            CurrentTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            RunElapsedSecondsText = "0";
        }

        private void ResetStatistics()
        {
            GoodCount = 0;
            FailCount = 0;
            ErrorCount = 0;
            TotalCount = 0;
            UpdateYieldRate();
        }

        private void UpdateProductionStatistics(string status)
        {
            TotalCount++;
            if (string.Equals(status, "Pass", StringComparison.OrdinalIgnoreCase))
            {
                GoodCount++;
            }
            else if (string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
            {
                ErrorCount++;
            }
            else
            {
                FailCount++;
            }

            UpdateYieldRate();
        }

        private void UpdateYieldRate()
        {
            YieldRatePercent = TotalCount == 0 ? 0.0 : GoodCount * 100.0 / TotalCount;
            YieldRateText = YieldRatePercent.ToString("0.00", CultureInfo.InvariantCulture) + "%";
        }

        private void InitializeResultRows(IReadOnlyList<TestItem> testItems)
        {
            Results.Clear();
            FailureResults.Clear();
            foreach (var item in testItems)
            {
                Results.Add(StepResultViewModel.Pending(item.Id, item.Name));
            }
        }

        private void MarkResultRunning(TestItem item)
        {
            var application = Application.Current;
            if (application?.Dispatcher != null && !application.Dispatcher.CheckAccess())
            {
                application.Dispatcher.Invoke(() => MarkResultRunningCore(item));
                return;
            }

            MarkResultRunningCore(item);
        }

        private void MarkResultRunningCore(TestItem item)
        {
            var row = FindResultRow(item.Id, item.Name);
            if (row == null)
            {
                row = StepResultViewModel.Pending(item.Id, item.Name);
                Results.Add(row);
            }

            row.MarkRunning();
            CurrentStep = item.Name;
        }

        private void MarkResultCompleted(TestItemResult result, int totalSteps)
        {
            var application = Application.Current;
            if (application?.Dispatcher != null && !application.Dispatcher.CheckAccess())
            {
                application.Dispatcher.Invoke(() => MarkResultCompletedCore(result, totalSteps));
                return;
            }

            MarkResultCompletedCore(result, totalSteps);
        }

        private void MarkResultCompletedCore(TestItemResult result, int totalSteps)
        {
            var row = FindResultRow(result.ItemId, result.ItemName);
            if (row == null)
            {
                row = StepResultViewModel.Pending(result.ItemId, result.ItemName);
                Results.Add(row);
            }

            row.MarkCompleted(ToStepResult(result));
            SyncFailureResult(row);
            CurrentStep = result.ItemName;
            var completedCount = Results.Count(x => x.IsCompleted);
            ProgressText = completedCount + " / " + totalSteps;
            ProgressPercent = totalSteps <= 0 ? 0.0 : completedCount * 100.0 / totalSteps;
        }

        private void SyncFailureResult(StepResultViewModel row)
        {
            if (row.IsFailure)
            {
                if (!FailureResults.Contains(row))
                {
                    FailureResults.Add(row);
                }
            }
            else if (FailureResults.Contains(row))
            {
                FailureResults.Remove(row);
            }
        }

        private void AddRunBlockerFailure(string stepName, string message)
        {
            FailureResults.Add(StepResultViewModel.FromResult(new StepResult
            {
                StepName = stepName,
                Status = StepStatus.Error,
                Message = message,
                StartTime = DateTimeOffset.Now,
                EndTime = DateTimeOffset.Now
            }));
        }

        private static string BuildWorkflowStatusReason(WorkflowResult workflowResult)
        {
            if (workflowResult.Passed)
            {
                return string.Empty;
            }

            var failedResult = workflowResult.Results.FirstOrDefault(x =>
                x.Status == StepStatus.Failed
                || x.Status == StepStatus.Error
                || x.Status == StepStatus.Stopped
                || x.Status == StepStatus.Terminated);
            if (failedResult == null)
            {
                return string.Empty;
            }

            var message = failedResult.Message ?? failedResult.Error?.Message ?? string.Empty;
            return string.IsNullOrWhiteSpace(message)
                ? failedResult.ItemName + " " + failedResult.Status
                : failedResult.ItemName + ": " + message;
        }

        private StepResultViewModel? FindResultRow(string itemId, string itemName)
        {
            return Results.FirstOrDefault(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                ?? Results.FirstOrDefault(x => string.Equals(x.StepName, itemName, StringComparison.OrdinalIgnoreCase));
        }

        private void AddResult(StepResult result, int totalSteps)
        {
            var application = Application.Current;
            if (application?.Dispatcher != null && !application.Dispatcher.CheckAccess())
            {
                application.Dispatcher.Invoke(() => AddResultCore(result, totalSteps));
                return;
            }

            AddResultCore(result, totalSteps);
        }

        private void AddResultCore(StepResult result, int totalSteps)
        {
            var row = StepResultViewModel.FromResult(result);
            Results.Add(row);
            SyncFailureResult(row);
            CurrentStep = result.StepName;
            ProgressText = Results.Count + " / " + totalSteps;
            ProgressPercent = totalSteps <= 0 ? 0.0 : Results.Count * 100.0 / totalSteps;
        }

        private IStationAdapterRegistry CreateAdapterRegistry(string repoRoot)
        {
            return string.Equals(ExecutionMode, "Hardware", StringComparison.OrdinalIgnoreCase)
                ? (IStationAdapterRegistry)new HardwareStationAdapterRegistry(repoRoot)
                : new MockStationAdapterRegistry();
        }

        private static StepResult ToStepResult(TestItemResult result)
        {
            return new StepResult
            {
                StepName = result.ItemName,
                Status = result.Status,
                Value = result.Value,
                Unit = result.Unit,
                LowLimit = result.LowLimit,
                HighLimit = result.HighLimit,
                ExpectedValue = result.ExpectedValue,
                CompareType = result.CompareType,
                Target = result.Target,
                Message = result.Message,
                ExternalLogPath = result.ExternalLogPath,
                StartTime = result.StartTime,
                EndTime = result.EndTime,
                Error = result.Error
            };
        }

        private static string FormatStepLog(int index, int totalSteps, TestItemResult result)
        {
            var durationMs = result.StartTime == default(DateTimeOffset) || result.EndTime == default(DateTimeOffset)
                ? string.Empty
                : " DurationMs=" + Math.Round((result.EndTime - result.StartTime).TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture);
            var message = string.IsNullOrWhiteSpace(result.Message) ? string.Empty : " Message=" + result.Message;

            return "Step "
                + index.ToString(CultureInfo.InvariantCulture)
                + "/"
                + totalSteps.ToString(CultureInfo.InvariantCulture)
                + " "
                + result.ItemName
                + " "
                + result.Status
                + durationMs
                + message;
        }

        private void WriteReports(
            string reportDirectory,
            DateTimeOffset startedAt,
            DateTimeOffset finishedAt,
            string testPlanName,
            string testPlanPath,
            string configPath,
            System.Collections.Generic.IEnumerable<TestItem> testItems,
            System.Collections.Generic.IEnumerable<TestItemResult> workflowResults)
        {
            var resultList = workflowResults.ToList();
            var itemById = testItems
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var report = new RunReport
            {
                SerialNumber = SerialNumber,
                TestPlanName = testPlanName,
                TestPlanPath = testPlanPath,
                ConfigPath = configPath,
                ExecutionMode = ExecutionMode,
                Operator = CurrentUser,
                StartedAt = startedAt,
                FinishedAt = finishedAt,
                Passed = resultList.All(x => x.Status == StepStatus.Passed)
            };

            foreach (var result in resultList)
            {
                TestItem item;
                if (itemById.TryGetValue(result.ItemId, out item))
                {
                    report.StepItems.Add(item);
                }

                report.StepResults.Add(ToStepResult(result));
            }

            var jsonPath = new JsonResultWriter().Write(reportDirectory, report);
            var csvPath = new CsvResultWriter().Write(reportDirectory, report);
            var logPath = new RunLogWriter().Write(reportDirectory, report);
            Logs.Add("JSON report: " + jsonPath);
            Logs.Add("CSV report: " + csvPath);
            Logs.Add("Run log: " + logPath);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var testPlan = Path.Combine(dir.FullName, "Runtime", "TestPlans", "Rfp7000V2.testplan.json");
                if (File.Exists(testPlan))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Cannot locate Runtime/TestPlans/Rfp7000V2.testplan.json.");
        }

        private bool IsEnglish
        {
            get { return string.Equals(SelectedLanguage, "English", StringComparison.OrdinalIgnoreCase); }
        }

        private string T(string chinese, string english)
        {
            return IsEnglish ? english : chinese;
        }

        private string TranslateStatus(string status)
        {
            if (IsEnglish)
            {
                if (status == "SafetyStop")
                {
                    return "Safety Stop";
                }

                return status;
            }

            switch (status)
            {
                case "Idle":
                    return "待机";
                case "Running":
                    return "运行中";
                case "Pass":
                    return "通过";
                case "Fail":
                    return "失败";
                case "Error":
                    return "错误";
                case "Stopped":
                    return "已停止";
                case "SafetyStop":
                    return "安全停机";
                default:
                    return status;
            }
        }

        private void RaiseLocalizedPropertiesChanged()
        {
            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(CurrentUserLabel));
            OnPropertyChanged(nameof(ProductNameLabel));
            OnPropertyChanged(nameof(LanguageLabel));
            OnPropertyChanged(nameof(TestPlanLabel));
            OnPropertyChanged(nameof(ConfigLabel));
            OnPropertyChanged(nameof(ExecutionModeLabel));
            OnPropertyChanged(nameof(ExecutionModeStatusText));
            OnPropertyChanged(nameof(MockScenarioLabel));
            OnPropertyChanged(nameof(MockScenarioVisibility));
            OnPropertyChanged(nameof(IsHardwareMode));
            OnPropertyChanged(nameof(HardwareModeWarningText));
            OnPropertyChanged(nameof(HardwareModeWarningVisibility));
            OnPropertyChanged(nameof(TestPlanEditorPageTitleText));
            OnPropertyChanged(nameof(StartupConfigTitleText));
            OnPropertyChanged(nameof(TestPlanItemsTitleText));
            OnPropertyChanged(nameof(ReloadTestPlanButtonText));
            OnPropertyChanged(nameof(SaveTestPlanButtonText));
            OnPropertyChanged(nameof(EditableTestPlanNameLabel));
            OnPropertyChanged(nameof(EditableTestPlanVersionLabel));
            OnPropertyChanged(nameof(EditableTestPlanProductLabel));
            OnPropertyChanged(nameof(SaveSettingsButtonText));
            OnPropertyChanged(nameof(SaveStationConfigButtonText));
            OnPropertyChanged(nameof(ReloadStationConfigButtonText));
            OnPropertyChanged(nameof(SettingsPageTitleText));
            OnPropertyChanged(nameof(CommonSettingsSectionText));
            OnPropertyChanged(nameof(StartupSettingsSectionText));
            OnPropertyChanged(nameof(FlashingSettingsSectionText));
            OnPropertyChanged(nameof(InstrumentSettingsSectionText));
            OnPropertyChanged(nameof(FctSettingsSectionText));
            OnPropertyChanged(nameof(SafetySettingsSectionText));
            OnPropertyChanged(nameof(AdvancedSettingsSectionText));
            OnPropertyChanged(nameof(SaveAllStationConfigButtonText));
            OnPropertyChanged(nameof(FlashConfigTitleText));
            OnPropertyChanged(nameof(InstrumentConfigTitleText));
            OnPropertyChanged(nameof(FctConfigTitleText));
            OnPropertyChanged(nameof(SafetyConfigTitleText));
            OnPropertyChanged(nameof(RfpFlashScriptLabel));
            OnPropertyChanged(nameof(TconFlashScriptLabel));
            OnPropertyChanged(nameof(TconBinFileLabel));
            OnPropertyChanged(nameof(TddiFlashScriptLabel));
            OnPropertyChanged(nameof(TddiSerialPortLabel));
            OnPropertyChanged(nameof(IoDaqComLabel));
            OnPropertyChanged(nameof(ScannerComLabel));
            OnPropertyChanged(nameof(OscilloscopeHostLabel));
            OnPropertyChanged(nameof(FctStationLabel));
            OnPropertyChanged(nameof(SafetyEnabledLabel));
            OnPropertyChanged(nameof(SafetyPollIntervalLabel));
            OnPropertyChanged(nameof(EmergencyStopChannelLabel));
            OnPropertyChanged(nameof(EmergencyStopTriggeredValueLabel));
            OnPropertyChanged(nameof(LightCurtainChannelLabel));
            OnPropertyChanged(nameof(LightCurtainTriggeredValueLabel));
            OnPropertyChanged(nameof(FixtureDownOutputChannelLabel));
            OnPropertyChanged(nameof(FixtureUpOutputChannelLabel));
            OnPropertyChanged(nameof(FixtureUpDelayLabel));
            OnPropertyChanged(nameof(OperationsLabel));
            OnPropertyChanged(nameof(LogoutButtonText));
            OnPropertyChanged(nameof(StartButtonText));
            OnPropertyChanged(nameof(PauseButtonText));
            OnPropertyChanged(nameof(EndButtonText));
            OnPropertyChanged(nameof(ProgressLabel));
            OnPropertyChanged(nameof(ElapsedTimeLabel));
            OnPropertyChanged(nameof(SerialNumberLabel));
            OnPropertyChanged(nameof(StatisticsLabel));
            OnPropertyChanged(nameof(GoodLabel));
            OnPropertyChanged(nameof(FailLabel));
            OnPropertyChanged(nameof(ErrorLabel));
            OnPropertyChanged(nameof(TotalLabel));
            OnPropertyChanged(nameof(YieldLabel));
            OnPropertyChanged(nameof(ResetStatisticsText));
            OnPropertyChanged(nameof(FooterSloganText));
            OnPropertyChanged(nameof(StepHeaderText));
            OnPropertyChanged(nameof(EnabledHeaderText));
            OnPropertyChanged(nameof(RequiredHeaderText));
            OnPropertyChanged(nameof(StopOnFailureHeaderText));
            OnPropertyChanged(nameof(TimeoutHeaderText));
            OnPropertyChanged(nameof(KindHeaderText));
            OnPropertyChanged(nameof(ScriptHeaderText));
            OnPropertyChanged(nameof(AdapterHeaderText));
            OnPropertyChanged(nameof(OperationHeaderText));
            OnPropertyChanged(nameof(NoteHeaderText));
            OnPropertyChanged(nameof(SummaryHeaderText));
            OnPropertyChanged(nameof(DescriptionHeaderText));
            OnPropertyChanged(nameof(StatusHeaderText));
            OnPropertyChanged(nameof(MeasurementHeaderText));
            OnPropertyChanged(nameof(ExpectedValueHeaderText));
            OnPropertyChanged(nameof(TargetHeaderText));
            OnPropertyChanged(nameof(UnitsHeaderText));
            OnPropertyChanged(nameof(LowLimitHeaderText));
            OnPropertyChanged(nameof(HighLimitHeaderText));
            OnPropertyChanged(nameof(ComparisonTypeHeaderText));
            OnPropertyChanged(nameof(OverallStatusText));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

    }
}
