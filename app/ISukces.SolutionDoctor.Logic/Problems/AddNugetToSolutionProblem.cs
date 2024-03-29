﻿using System;
using System.Collections.Generic;
using System.Linq;
using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class AddNugetToSolutionProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            var l = SuggestedNugets.OrderBy(a => a).Last();
            writeLine($"projects references {Dependency.Name} that is in some nuspec");
            if (!string.IsNullOrEmpty(ExtraInfo))
                writeLine(ExtraInfo);
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
            var p     = new SolutionProject { Location = ProjectFilename };
            if (p.Kind == VsProjectKind.Core)
            {
                var tmp = FixScript.CoreNugetInstall(ProjectFilename, nuget, "add");
                return tmp;
            }

            return FixScript.FullFxNugetInstall(ProjectFilename, nuget, "install");
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #region properties

        public ProjectReference   Dependency      { get; set; }
        public HashSet<PackageId> SuggestedNugets { get; set; }
        public string             ExtraInfo       { get; set; }

        #endregion
    }
}
