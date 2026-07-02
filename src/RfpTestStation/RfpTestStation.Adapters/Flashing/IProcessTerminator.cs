using System.Diagnostics;

namespace RfpTestStation.Adapters.Flashing
{
    public interface IProcessTerminator
    {
        void TerminateTree(Process process);
    }
}
