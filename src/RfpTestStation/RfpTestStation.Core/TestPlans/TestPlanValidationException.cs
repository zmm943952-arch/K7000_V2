using System;

namespace RfpTestStation.Core.TestPlans
{
    public sealed class TestPlanValidationException : Exception
    {
        public TestPlanValidationException(string message)
            : base(message)
        {
        }
    }
}
