using System;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class NuGetPackageShouldBeReferencedProblem : Problem
    {
        public Nuspec PackageToReference { get; set; }
        public ProjectReference ReferencedLibrary { get; set; }

        public override void Describe(Action<string> writeLine)
        {
            writeLine.WriteFormat("project references file {0}, but not references to NuGet package {1}", 
                ReferencedLibrary.HintPath.FullName,
                PackageToReference.FullId
                );
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }
    }
}