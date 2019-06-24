using System.Collections.Generic;

namespace ISukces.SolutionDoctor.Logic
{
    public enum AddRemoveOption
    {
        NoChange,
        Add,
        Remove
    }


    public interface IDoctorConfig
    {
        Dictionary<string, AddRemoveOption> NoWarn { get; }

        Dictionary<string, AddRemoveOption> WarningsAsErrors { get; }
        
        List<string> ExcludeDirectories { get; }
    }
}