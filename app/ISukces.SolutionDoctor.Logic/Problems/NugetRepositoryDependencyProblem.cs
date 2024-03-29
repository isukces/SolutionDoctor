using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class NugetRepositoryDependencyProblem : Problem, IConsiderUpdatePackage
    {
        public override void Describe(Action<RichString> writeLine)
        {
            var x = RichString.RichFormat(ctx =>
                {
                    if (ctx.ParameterIndex == 3)
                    {
                        if (ctx.Before)
                            ctx.Sink.Add(new RichString.ConsoleControl
                            {
                                TextColor = ConsoleColor.Cyan
                            });
                        else
                            ctx.ResetColors();
                    }
                },
                "projects references package {0}.{1} but package '{2}' requires {3}",
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

        public override FixScript GetFixScript()
        {
            return null;
        }
        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public string GetPackageId()
        {
            return CheckedPackageId;
        }

        #region properties

        public NugetVersionRange ReferencedPackageAcceptableVersions { get; set; }

        public NugetPackage PackageReferencedByProject { get; set; }

        public string CheckedPackageId { get; set; }

        public DirectoryInfo CheckedPackageLocation { get; set; }

        #endregion
    }
}
