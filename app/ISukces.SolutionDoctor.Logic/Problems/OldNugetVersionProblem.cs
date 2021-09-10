using System;
using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class OldNugetVersionProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            var c = new MessageColorer()
                .WithProjectAt(0)
                .WithPackageAt(1)
                .WithVersionAt(2, 3);

            var txt = RichString.RichFormat(
                ctx => c.Color(ctx),
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

        public override FixScript GetFixScript()
        {
            var p = new SolutionProject {Location = ProjectFilename};
            if (p.Kind == VsProjectKind.Core)
                return FixScript.CoreNugetInstall(ProjectFilename, new PackageId(PackageId, NewestVersion, ""), "add");
            
            return FixScript.FullFxNugetInstall(ProjectFilename, new PackageId(PackageId, NewestVersion, ""), "update");
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public NugetVersion ReferencedVersion { get; set; }
        public NugetVersion NewestVersion     { get; set; }
        public string       PackageId         { get; set; }
    }
}