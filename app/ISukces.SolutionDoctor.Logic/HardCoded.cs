using System;
using System.IO;

namespace ISukces.SolutionDoctor.Logic
{
    public class HardCoded
    {
        #region properties

        public static string Cache
        {
            get
            {
                var up = Environment.GetEnvironmentVariable("USERPROFILE");
                var s  = Path.Combine(up, @".nuget\packages");
                return s;
            }
        }

        #endregion
    }
}
