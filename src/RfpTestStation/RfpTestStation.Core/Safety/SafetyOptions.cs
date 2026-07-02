namespace RfpTestStation.Core.Safety
{
    public sealed class SafetyOptions
    {
        public bool Enabled { get; set; } = true;

        public int PollIntervalMs { get; set; } = 50;

        public SafetyInputOptions EmergencyStop { get; set; } = new SafetyInputOptions
        {
            Channel = 4,
            TriggeredValue = true
        };

        public SafetyInputOptions LightCurtain { get; set; } = new SafetyInputOptions
        {
            Channel = 5,
            TriggeredValue = false
        };

        public ReleaseFixtureOptions ReleaseFixture { get; set; } = new ReleaseFixtureOptions();

        public SafetyTriggerActionOptions OnTrigger { get; set; } = new SafetyTriggerActionOptions();

        public static SafetyOptions Default()
        {
            return new SafetyOptions();
        }
    }

    public sealed class SafetyInputOptions
    {
        public int Channel { get; set; }

        public bool TriggeredValue { get; set; }
    }

    public sealed class ReleaseFixtureOptions
    {
        public int DownOutputChannel { get; set; } = 2;

        public bool DownSafeValue { get; set; } = false;

        public int UpOutputChannel { get; set; } = 1;

        public bool UpPreValue { get; set; } = false;

        public int UpDelayMs { get; set; } = 1000;

        public bool UpSafeValue { get; set; } = true;
    }

    public sealed class SafetyTriggerActionOptions
    {
        public bool CancelRun { get; set; } = true;

        public bool KillExternalProcessTree { get; set; } = true;

        public bool LatchUntilManualReset { get; set; } = true;
    }
}
