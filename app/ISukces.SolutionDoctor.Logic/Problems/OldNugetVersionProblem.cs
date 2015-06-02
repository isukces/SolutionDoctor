using System;
using System.IO;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class OldNugetVersionProblem : Problem
    {
        #region Methods

        // Public Methods 

        public override void Describe(Action<string> writeLine)
        {
            var txt = string.Format(
                "Project {0} refers to old package {1} version ({2} instead of {3})",
                ProjectFilename.Name,
                PackageId,
                ReferencedVersion,
                NewestVersion
                );
            writeLine(txt);
        }

        public override ProblemFix GetFix()
        {
            return null;
        }
        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #endregion Methods

        #region Properties

        public NugetVersion ReferencedVersion { get; set; }
        public NugetVersion NewestVersion { get; set; }
        public string PackageId { get; set; }

        #endregion Properties
    }
}