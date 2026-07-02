using System;
using System.Collections.Generic;
using System.Linq;

namespace RfpTestStation.Core.Model
{
    public sealed class SequenceDocument
    {
        public IDictionary<string, object> FileGlobals { get; } = new Dictionary<string, object>();

        public IList<SequenceDefinition> Sequences { get; } = new List<SequenceDefinition>();

        public SequenceDefinition GetSequence(string name)
        {
            var sequence = Sequences.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (sequence == null)
            {
                throw new InvalidOperationException("Sequence not found: " + name);
            }

            return sequence;
        }
    }
}
