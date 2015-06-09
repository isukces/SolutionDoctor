using System;
using System.IO;
using ISukces.SolutionDoctor.Logic.NuGet;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class NugetRepositoryDependencyProblem : Problem
    {
        #region Methods

        // Public Methods 
        
        public override void Describe(Action<string> writeLine)
        {
            writeLine.WriteFormat("projects references package {0}.{1} but package '{2}' requires {3}",
                PackageReferencedByProject.Id,
                PackageReferencedByProject.Version,
                CheckedPackageId,
                ReferencedPackageAcceptableVersions
                );
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

        public NugetVersionRange ReferencedPackageAcceptableVersions { get; set; }

        public NugetPackage PackageReferencedByProject { get; set; }

        public string CheckedPackageId { get; set; }

        public DirectoryInfo CheckedPackageLocation { get; set; }

        #endregion Properties
    }
}