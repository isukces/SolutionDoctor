using System;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class SolutionsInManyFoldersProblem : Problem
    {
        #region Methods

        // Public Methods 

        public override void Describe(Action<string> writeLine)
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

        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return ProjectHasNugetPackages;
        }

        #endregion Methods

        #region Properties

        public FileName[] Solutions { get; set; }

        public bool ProjectHasNugetPackages { get; set; }

        #endregion Properties
    }
}