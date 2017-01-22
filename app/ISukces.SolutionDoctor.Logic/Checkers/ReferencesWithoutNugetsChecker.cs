using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    class ReferencesWithoutNugetsChecker
    {
        #region Constructors

        public ReferencesWithoutNugetsChecker()
        {
        }

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static IList<Problem> Check(IEnumerable<Project> projects, IEnumerable<Nuspec> localNugetRepositiories)
        {
            // var aa = localNugetRepositiories.GetUnique(a => a.Location.FullName.ToLower(), a => a);

            var checker = new ReferencesWithoutNugetsChecker
            {
                _projects = projects.ToList(), // .Select(a => a.Projects.First()).ToList()
                _nuspecs = (from nuspec in localNugetRepositiories
                            let dir =
                                nuspec.Location.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar
                            select Tuple.Create(dir.ToLower(), nuspec)).ToArray(),
            };

            checker._nuspecs = checker._nuspecs.GetUnique(a => a.Item1, a => a)
#if DEBUG
.OrderBy(a => a.Item1)
#endif
.ToArray();

            return checker.Check();

        }
        // Private Methods 

        private static Dictionary<string, HashSet<string>> GetNuspecDllMap(Tuple<string, Nuspec>[] nuspecs)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var nuspec in nuspecs)
            {
                var tmp = ScanDll(nuspec.Item1);
                foreach (var ii in tmp)
                {
                    HashSet<string> list;
                    if (!map.TryGetValue(ii, out list))
                    {
                        list = new HashSet<string>();
                        map[ii] = list;
                    }
                    list.Add(nuspec.Item2.FullId);
                }
            }
            return map;
        }

        private static HashSet<string> ScanDll(string dirName)
        {
            var directoryInfo = new DirectoryInfo(dirName);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!directoryInfo.Exists)
                return result;
            foreach (var dll in directoryInfo.GetFiles("*.dll").Select(a => a.Name))
                result.Add(dll);
            foreach (var dir in directoryInfo.GetDirectories())
            {
                var dlls = ScanDll(dir.FullName);
                foreach (var dll in dlls)
                    result.Add(dll);
            }
            return result;
        }

        #endregion Static Methods

        #region Methods

        // Private Methods 

        private IList<Problem> Check()
        {
            _map = GetNuspecDllMap(_nuspecs);
            var a = _projects.SelectMany(CheckProj).ToList();
            return a;
        }

        private IEnumerable<Problem> CheckProj(Project project)
        {
            var projectReferences = project.References.Where(a => a.HintPath != null).ToArray();
            if (!projectReferences.Any())
                yield break;
            foreach (var dep in projectReferences)
            {
                var nuspec = FindNuspecByFile(dep.HintPath);

                if (nuspec == null)
                {
                    HashSet<string> list;
                    string depName = GetDepDll(dep);
                    if (_map.TryGetValue(depName, out list) && list.Any())
                    {
                        yield return new AddNugetToSolutionProblem
                        {
                            ProjectFilename = project.Location,
                            Dependency = dep,
                            SuggestedNugets = list
                        };
                    }
                    continue;
                }
                var candidates = project.NugetPackages
                    .Where(a => a.Id == nuspec.Id)
                    .ToArray();

                var nugetPackage = candidates
                    .FirstOrDefault(a => a.Version == nuspec.PackageVersion);
                if (nugetPackage != null) continue;
                yield return new NuGetPackageShouldBeReferencedProblem
                {
                    ProjectFilename = project.Location,
                    PackageToReference = nuspec,
                    ReferencedLibrary = dep
                };
            }
        }

        private static string GetDepDll(ProjectReference dep)
        {
            if (dep.HintPath == null)
                return null;
            return dep.HintPath.Name;
        }

        private Nuspec FindNuspecByFile([NotNull] FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            var hintToLower = file.FullName.ToLower();
            var query = from nuspec in _nuspecs
                        where hintToLower.StartsWith(nuspec.Item1)
                        select nuspec.Item2;
            return query.FirstOrDefault();
        }

        #endregion Methods

        #region Fields

        private Dictionary<string, HashSet<string>> _map;
        private Tuple<string, Nuspec>[] _nuspecs;
        List<Project> _projects;

        #endregion Fields
    }
}