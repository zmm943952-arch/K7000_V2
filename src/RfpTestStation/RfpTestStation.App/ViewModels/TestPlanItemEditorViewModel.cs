using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
            set
            {
                if (SetField(ref _parametersJson, value))
                {
                    OnDisplayPropertiesChanged();
                }
            }
        }

        public bool IsFunctionalGroup
        {
            get { return string.Equals(ReadDisplayParameter("template"), "I2cFunctionalGroup", StringComparison.OrdinalIgnoreCase); }
        }

        public string SummaryText
        {
            get
            {
                if (IsFunctionalGroup)
                {
                    var childCount = ReadGroupChildren().Count;
                    var powerText = ReadSharedPowerText();
                    return childCount.ToString(CultureInfo.InvariantCulture) + " child items"
                        + (string.IsNullOrWhiteSpace(powerText) ? string.Empty : "; shared power: " + powerText);
                }

                if (!string.IsNullOrWhiteSpace(Script))
                {
                    return "Script: " + System.IO.Path.GetFileName(Script);
                }

                if (!string.IsNullOrWhiteSpace(Adapter) || !string.IsNullOrWhiteSpace(Operation))
                {
                    return string.Join(" / ", new[] { Adapter, Operation }.Where(x => !string.IsNullOrWhiteSpace(x)));
                }

                if (!string.IsNullOrWhiteSpace(LowLimit) || !string.IsNullOrWhiteSpace(HighLimit))
                {
                    return "Limit: " + LowLimit + " .. " + HighLimit + (string.IsNullOrWhiteSpace(Unit) ? string.Empty : " " + Unit);
                }

                return string.Empty;
            }
        }

        public string GroupChildrenText
        {
            get
            {
                var names = ReadGroupChildren()
                    .Select(x => ReadObjectString(x, "name"))
                    .Where(x => !string.IsNullOrWhiteSpace(x));
                return string.Join(Environment.NewLine, names);
            }
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

        private string ReadDisplayParameter(string name)
        {
            var parameters = TryParseParameters();
            return parameters == null ? string.Empty : ReadParameter(parameters, name);
        }

        private string ReadSharedPowerText()
        {
            var parameters = TryParseParameters();
            var powerItems = parameters?["powerOnBefore"] as JArray;
            if (powerItems == null || powerItems.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var token in powerItems.OfType<JObject>())
            {
                var channel = ReadObjectString(token, "channel");
                var voltage = ReadObjectString(token, "voltage");
                if (!string.IsNullOrWhiteSpace(channel))
                {
                    parts.Add("CH" + channel + (string.IsNullOrWhiteSpace(voltage) ? string.Empty : " " + voltage + "V"));
                }
            }

            return string.Join(", ", parts);
        }

        private IList<JObject> ReadGroupChildren()
        {
            var parameters = TryParseParameters();
            var children = parameters?["items"] as JArray;
            return children == null
                ? new List<JObject>()
                : children.OfType<JObject>().ToList();
        }

        private JObject? TryParseParameters()
        {
            if (string.IsNullOrWhiteSpace(ParametersJson))
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(ParametersJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string ReadObjectString(JObject item, string name)
        {
            var token = item[name];
            return token == null ? string.Empty : token.ToString();
        }

        private void OnDisplayPropertiesChanged()
        {
            OnPropertyChanged(nameof(IsFunctionalGroup));
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(GroupChildrenText));
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
            OnPropertyChanged(propertyName ?? string.Empty);
            if (propertyName == nameof(Name)
                || propertyName == nameof(KindText)
                || propertyName == nameof(TimeoutSeconds)
                || propertyName == nameof(Script)
                || propertyName == nameof(Adapter)
                || propertyName == nameof(Operation)
                || propertyName == nameof(LowLimit)
                || propertyName == nameof(HighLimit)
                || propertyName == nameof(Unit)
                || propertyName == nameof(ParametersJson))
            {
                OnDisplayPropertiesChanged();
            }

            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
