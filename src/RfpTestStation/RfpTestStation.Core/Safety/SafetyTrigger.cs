using System;

namespace RfpTestStation.Core.Safety
{
    public enum SafetyTriggerKind
    {
        EmergencyStop,
        LightCurtain
    }

    public sealed class SafetyTrigger
    {
        public SafetyTrigger(SafetyTriggerKind kind, int channel, bool value)
        {
            Kind = kind;
            Channel = channel;
            Value = value;
            TriggeredAt = DateTimeOffset.UtcNow;
        }

        public SafetyTriggerKind Kind { get; }

        public int Channel { get; }

        public bool Value { get; }

        public DateTimeOffset TriggeredAt { get; }

        public string Message
        {
            get { return Kind + " triggered on channel " + Channel + " value=" + Value; }
        }
    }
}
