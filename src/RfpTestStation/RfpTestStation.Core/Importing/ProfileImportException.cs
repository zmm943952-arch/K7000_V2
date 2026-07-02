using System;

namespace RfpTestStation.Core.Importing
{
    public sealed class ProfileImportException : Exception
    {
        public ProfileImportException(string message)
            : base(message)
        {
        }

        public ProfileImportException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
