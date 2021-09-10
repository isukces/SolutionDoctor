using System;
using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class SolutionsInManyFoldersProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine("is part of solutions located in different folders folders");
            foreach (var folder in Solutions)
                writeLine("    " + folder);
            if (ProjectHasNugetPackages)
                writeLine("Project references nuget packages !!!");
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        public override FixScript GetFixScript()
        {
            return null;
        }


        protected override bool GetIsBigProblem()
        {
            return ProjectHasNugetPackages;
        }

        public FileName[] Solutions { get; set; }

        public bool ProjectHasNugetPackages { get; set; }
    }
}