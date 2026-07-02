using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RfpTestStation.Core.TestPlans;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.App.ViewModels
{
    public sealed class TestPlanItemEditorViewModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _kindText = TestItemKind.Measurement.ToString();
        private bool _isEnabled = true;
        private bool _isRequired = true;
        private bool _stopOnFailure = true;
        private int _timeoutSeconds = 30;
        private string _sourceReference = string.Empty;
        private string _flashKind = string.Empty;
        private string _script = string.Empty;
        private string _adapter = string.Empty;
        private string _operation = string.Empty;
        private string _lowLimit = string.Empty;
        private string _highLimit = string.Empty;
        private string _unit = string.Empty;
        private string _comparisonType = string.Empty;
        private string _note = string.Empty;
        private string _parametersJson = "{}";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id
        {
            get { return _id; }
            set { SetField(ref _id, value); }
        }

        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }

        public string KindText
        {
            get { return _kindText; }
            set { SetField(ref _kindText, value); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value); }
        }

        public bool IsRequired
        {
            get { return _isRequired; }
            set { SetField(ref _isRequired, value); }
        }

        public bool StopOnFailure
        {
            get { return _stopOnFailure; }
            set { SetField(ref _stopOnFailure, value); }
        }

        public int TimeoutSeconds
        {
            get { return _timeoutSeconds; }
            set { SetField(ref _timeoutSeconds, value); }
        }

        public string SourceReference
        {
            get { return _sourceReference; }
            set { SetField(ref _sourceReference, value); }
        }

        public string FlashKind
        {
            get { return _flashKind; }
            set { SetField(ref _flashKind, value); }
        }

        public string Script
        {
            get { return _script; }
            set { SetField(ref _script, value); }
        }

        public string Adapter
        {
            get { return _adapter; }
            set { SetField(ref _adapter, value); }
        }

        public string Operation
        {
            get { return _operation; }
            set { SetField(ref _operation, value); }
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

        public string ComparisonType
        {
            get { return _comparisonType; }
            set { SetField(ref _comparisonType, value); }
        }

        public string Note
        {
            get { return _note; }
            set { SetField(ref _note, value); }
        }

        public string ParametersJson
        {
            get { return _parametersJson; }
            set { SetField(ref _parametersJson, value); }
        }

        public static TestPlanItemEditorViewModel FromDefinition(TestPlanItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var parameters = definition.Parameters == null
                ? new JObject()
                : (JObject)definition.Parameters.DeepClone();
            return new TestPlanItemEditorViewModel
            {
                Id = definition.Id,
                Name = definition.Name,
                KindText = definition.Kind.ToString(),
                IsEnabled = definition.IsEnabled,
                IsRequired = definition.IsRequired,
                StopOnFailure = definition.StopOnFailure,
                TimeoutSeconds = definition.TimeoutSeconds,
                SourceReference = definition.SourceReference,
                FlashKind = ReadParameter(parameters, "flashKind"),
                Script = ReadParameter(parameters, "script"),
                Adapter = ReadParameter(parameters, "adapter"),
                Operation = ReadParameter(parameters, "operation"),
                LowLimit = ReadParameter(parameters, "low"),
                HighLimit = ReadParameter(parameters, "high"),
                Unit = ReadParameter(parameters, "unit"),
                ComparisonType = ReadParameter(parameters, "comparisonType"),
                Note = ReadParameter(parameters, "note"),
                ParametersJson = parameters.ToString(Formatting.None)
            };
        }

        public TestPlanItemDefinition ToDefinition()
        {
            if (!Enum.TryParse(KindText, true, out TestItemKind kind))
            {
                throw new TestPlanValidationException("Test plan item " + Id + ": kind is invalid.");
            }

            var parameters = ParseParametersJson();
            SetStringParameter(parameters, "flashKind", FlashKind);
            SetStringParameter(parameters, "script", Script);
            SetStringParameter(parameters, "adapter", Adapter);
            SetStringParameter(parameters, "operation", Operation);
            SetNumberOrStringParameter(parameters, "low", LowLimit);
            SetNumberOrStringParameter(parameters, "high", HighLimit);
            SetStringParameter(parameters, "unit", Unit);
            SetStringParameter(parameters, "comparisonType", ComparisonType);
            SetStringParameter(parameters, "note", Note);

            return new TestPlanItemDefinition
            {
                Id = Id,
                Name = Name,
                Kind = kind,
                IsEnabled = IsEnabled,
                IsRequired = IsRequired,
                StopOnFailure = StopOnFailure,
                TimeoutSeconds = TimeoutSeconds,
                SourceReference = SourceReference,
                Parameters = parameters
            };
        }

        private JObject ParseParametersJson()
        {
            if (string.IsNullOrWhiteSpace(ParametersJson))
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(ParametersJson);
            }
            catch (JsonException ex)
            {
                throw new TestPlanValidationException("Parameters JSON is invalid for item " + Id + ": " + ex.Message);
            }
        }

        private static string ReadParameter(JObject parameters, string name)
        {
            var token = parameters[name];
            if (token == null)
            {
                return string.Empty;
            }

            return token.Type == JTokenType.String
                ? token.ToObject<string>() ?? string.Empty
                : token.ToString(Formatting.None);
        }

        private static void SetStringParameter(JObject parameters, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parameters.Remove(name);
                return;
            }

            parameters[name] = value;
        }

        private static void SetNumberOrStringParameter(JObject parameters, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parameters.Remove(name);
                return;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
            {
                parameters[name] = numericValue;
                return;
            }

            parameters[name] = value;
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
            return true;
        }
    }
}
