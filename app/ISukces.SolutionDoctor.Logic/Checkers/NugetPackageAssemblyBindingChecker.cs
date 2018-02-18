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
    internal static class NugetPackageAssemblyBindingChecker
    {
        public static IList<Problem> Check([NotNull] IList<Project> projects)
        {
            if (projects == null) throw new ArgumentNullException(nameof(projects));
            return projects.SelectMany(ScanProject).ToList();
        }

        private static IEnumerable<Problem> ScanProject(Project project)
        {
            var assemblyBindings = project.AssemblyBindings;
            var packageVersion   = project.NugetPackages;
            if (!assemblyBindings.Any() || !packageVersion.Any()) yield break;
            foreach (var package in packageVersion)
            {
                if (project.Kind == CsProjectKind.New)
                {
                    var redirects = assemblyBindings.Where(
                            a => string.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    foreach (var i in redirects)
                    {
                        if (i.Name != package.Id)
                        {
                            yield return new AssemblyRedirectionInvalidPackageId(package.Id, project);                            
                        }
                    }

                    continue;
                }
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
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                        $"Too many hint paths to dll for {package.Id} ver {package.Version} package. Probably package contains more than one dll inside.");
                    continue;
                }

                var versions = dlls.Select(GetDllInfo).Distinct().ToArray();
                {
                    var aa = versions.Where(q => q.DllVersion == null || !q.Exists).ToArray();
                    if (aa.Any())
                    {
                        foreach (var aaa in aa)
                            yield return
                                new UnableToGetReferencedDllVersionProblem(package.Id, project,
                                    "Broken file " + aaa?.File);
                        continue;
                    }
                }
                if (versions.Length != 1)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                        "Too many possible versions to compare");
                    continue;
                }

                if (versions.Any(a => a.DllVersion == redirect.NewVersion.NormalizedVersion.ToString())) continue;

                yield return new WrongBindingRedirectProblem
                {
                    ProjectFilename = project.Location,
                    Redirect        = redirect,
                    Package         = package,
                    DllVersion      = versions[0].DllVersion
                };
            }
        }

        private class DllInfo
        {
            public DllInfo(FileInfo file, string dllVersion, bool exists)
            {
                File    = file;
                DllVersion = dllVersion;
                Exists  = exists;
            }

            public FileInfo File    { get; set; }
            public string   DllVersion { get; set; }
            public bool     Exists  { get; set; }
        }

        private static DllInfo GetDllInfo(FileInfo file)
        {
            if (!file.Exists)
                return new DllInfo(file, null, false);
            try
            {
                var currentAssemblyName = AssemblyName.GetAssemblyName(file.FullName);
                return
                    new DllInfo(file,
                        currentAssemblyName.Version.ToString(),
                        true);
            }
            catch
            {
                return new DllInfo(file, null, true);
            }
        }

        private static Func<FileInfo, bool> ContainsPackagePath(NugetPackage package)
        {
            var path = "\\packages\\" + package.Id + "." + package.Version + "\\";
            return a => a != null && a.FullName.ToLower().Contains(path.ToLowerInvariant());
        }
    }
}