using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    class AddNugetToSolutionProblem : Problem
    {
        public override void Describe(Action<string> writeLine)
        {
            writeLine.WriteFormat("projects references {0} that is in some nuspec",
                 Dependency.Name);
            writeLine.WriteFormat("   Add nuspec, i.e. {0}", SuggestedNugets.First());
            writeLine.WriteFormat("   Install-Package {0}", SuggestedNugets.First());
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
        public HashSet<string> SuggestedNugets { get; set; }
    }
}
