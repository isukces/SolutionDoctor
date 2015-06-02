using System;
using System.Collections.Generic;
using System.Linq;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    static class NugetPackageAssemblyBindingChecker
    {
        #region Static Methods

        // Public Methods 

        public static IEnumerable<Problem> Check([NotNull] IList<Project> projects)
        {
            if (projects == null) throw new ArgumentNullException("projects");
            return projects.SelectMany(ScanProject);
        }

        // Private Methods 

        private static IEnumerable<Problem> ScanProject(Project project)
        {
            var assemblyBindings = project.AssemblyBindings;
            var packageVersion = project.NugetPackages;
            if (!assemblyBindings.Any() || !packageVersion.Any()) yield break;
            foreach (var package in packageVersion)
            {
                var redirect =
                    assemblyBindings.FirstOrDefault(
                        a => String.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase));
                if (redirect == null || redirect.NewVersion == package.Version) continue;
                yield return new WrongBindingRedirectProblem
                {
                    ProjectFilename = project.Location,
                    Redirect = redirect,
                    Package = package
                };
            }
        }

        #endregion Static Methods
    }
}