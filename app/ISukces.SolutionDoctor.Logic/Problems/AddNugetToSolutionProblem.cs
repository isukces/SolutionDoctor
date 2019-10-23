using System;
using System.Collections.Generic;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class AddNugetToSolutionProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            var l = SuggestedNugets.OrderBy(a => a).Last();
            writeLine($"projects references {Dependency.Name} that is in some nuspec");
            //writeLine("   Add nuspec, i.e. " + l.FullId);
            //var name = ProjectFilename.GetShortNameWithoutExtension();
            // writeLine($"   Install-Package {l.Id} -Version {l.PackageVersion} -ProjectName {name}");
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        public override FixScript GetFixScript()
        {
            var nuget = SuggestedNugets.OrderBy(a => a).Last();
            var p = new Project {Location = ProjectFilename};
            if (p.Kind == CsProjectKind.New)
                return FixScript.CoreNugetInstall(ProjectFilename, nuget, "add");
            return FixScript.FullFxNugetInstall(ProjectFilename, nuget, "install");
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public ProjectReference   Dependency      { get; set; }
        public HashSet<PackageId> SuggestedNugets { get; set; }
    }
}