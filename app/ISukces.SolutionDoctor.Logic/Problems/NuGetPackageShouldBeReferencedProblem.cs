using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class NuGetPackageShouldBeReferencedProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            var c = new MessageColorer()
                .WithProjectAt(0)
                .WithPackageAt(1);

            var txt = RichString.RichFormat(
                c.Color,
                "project references file {0}, but not references to NuGet package {1}",
                ReferencedLibrary.HintPath.FullName, PackageToReference.FullId);

            writeLine(txt);
            if (!string.IsNullOrWhiteSpace(VersionProblem))
                writeLine(VersionProblem);
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        public override FixScript GetFixScript()
        {
            var p = new SolutionProject { Location = ProjectFilename };
            if (p.Kind == VsProjectKind.Core)
                return FixScript.CoreNugetInstall(ProjectFilename, PackageToReference, "add");

            return FixScript.FullFxNugetInstall(ProjectFilename, PackageToReference, "install");
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #region properties

        public Nuspec           PackageToReference { get; set; }
        public ProjectReference ReferencedLibrary  { get; set; }
        public bool             IsCoreProject      { get; set; }
        public string           VersionProblem     { get; set; }

        #endregion
    }
}
