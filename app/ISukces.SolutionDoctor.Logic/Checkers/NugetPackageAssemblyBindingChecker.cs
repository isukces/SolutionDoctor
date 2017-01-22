using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    static class NugetPackageAssemblyBindingChecker
    {
        #region Static Methods

        // Public Methods 

        public static IList<Problem> Check([NotNull] IList<Project> projects)
        {
            if (projects == null) throw new ArgumentNullException("projects");
            return projects.SelectMany(ScanProject).ToList();
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
                        a => string.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase));
                if (redirect == null) continue;
                var dlls =
                  project.References.Select(a => a.HintPath)
                      .Where(ContainsPackagePath(package))
                      .ToArray();
                if (dlls.Length == 0)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project, "no hint path to dll");
                    continue;
                }
                if (dlls.Length > 1)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project, "too many hint paths to dll");
                    continue;
                }
                Tuple<FileInfo, string>[] versions = dlls.Select(a =>
                {
                    if (!a.Exists)
                        return null;
                    try
                    {
                        var currentAssemblyName = AssemblyName.GetAssemblyName(a.FullName);
                        return
                            new Tuple<FileInfo, string>(a,
                                currentAssemblyName.Version.ToString());
                    }
                    catch
                    {
                        return new Tuple<FileInfo, string>(a, null);
                    }
                }).Distinct().ToArray();
                {
                    var aa = versions.Where(q => q.Item2 == null).ToArray();
                    if (aa.Any())
                    {
                        foreach (var aaa in aa)
                            yield return
                                new UnableToGetReferencedDllVersionProblem(package.Id, project,
                                    "Broken file " + aaa.Item1);
                        continue;
                    }
                }
                if (versions.Length != 1)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,"Too many possible versions to compare");
                    continue;
                }
                if (versions.Any(a=>a.Item2==redirect.NewVersion.NormalizedVersion.ToString())) continue;

                yield return new WrongBindingRedirectProblem
                {
                    ProjectFilename = project.Location,
                    Redirect = redirect,
                    Package = package,
                    DllVersion = versions[0].Item2
                };
            }
        }

        private static Func<FileInfo, bool> ContainsPackagePath(NugetPackage package)
        {
            var path = "\\packages\\" + package.Id + "." + package.Version + "\\";
            return a => a != null && a.FullName.ToLower().Contains(path.ToLowerInvariant());
        }

        #endregion Static Methods
    }
}