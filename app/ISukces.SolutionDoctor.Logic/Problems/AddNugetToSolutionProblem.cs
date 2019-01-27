using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    class AddNugetToSolutionProblem : Problem
    {
        public override void Describe(Action<string> writeLine)
        {
            var l = SuggestedNugets.OrderBy(a => a).Last();
            writeLine.WriteFormat("projects references {0} that is in some nuspec",
                 Dependency.Name);
            writeLine.WriteFormat("   Add nuspec, i.e. {0}", l.FullId);
            var name = ProjectFilename.GetShortNameWithoutExtension();
            writeLine.WriteFormat("   Install-Package {0} -Version {1} -ProjectName {2}", l.Id, l.PackageVersion, name);
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public ProjectReference Dependency { get; set; }
        public HashSet<PackageId> SuggestedNugets { get; set; }
    }
}
