using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iSukces.Code.vssolutions;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class ReferencesWithoutNugetsChecker
    {
        #region Constructors

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static IEnumerable<Problem> Check(List<SolutionProject> projects,
            IEnumerable<Nuspec> localNugetRepositiories,
            HashSet<string> excludeDll)
        {
            // var aa = localNugetRepositiories.GetUnique(a => a.Location.FullName.ToLower(), a => a);

            var checker = new ReferencesWithoutNugetsChecker
            {
                _excludeDll = excludeDll ?? new HashSet<string>(),
                _projects   = projects.ToList(), // .Select(a => a.Projects.First()).ToList()
                _nuspecs = (from nuspec in localNugetRepositiories
                    let dir =
                        nuspec.Location.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar
                    select Tuple.Create(dir.ToLower(), nuspec)).ToArray()
            };

            checker._nuspecs = checker._nuspecs.GetUnique(a => a.Item1, a => a)
#if DEBUG
                .OrderBy(a => a.Item1)
#endif
                .ToArray();

            return checker.Check();
        }
        // Private Methods 

        private static Dictionary<string, HashSet<PackageId>> GetNuspecDllMap(Tuple<string, Nuspec>[] nuspecs)
        {
            var map = new Dictionary<string, HashSet<PackageId>>(StringComparer.OrdinalIgnoreCase);
            foreach (var nuspec in nuspecs)
            {
                var tmp = _cache.Find(nuspec);
                foreach (var ii in tmp)
                {
                    if (!map.TryGetValue(ii, out var list))
                    {
                        list    = new HashSet<PackageId>();
                        map[ii] = list;
                    }

                    list.Add(nuspec.Item2.GetPackageId());
                }
            }

            return map;
        }

        private class Cache
        {
            public HashSet<string> Find(Tuple<string, Nuspec> nuspec)
            {
                lock(l)
                {
                    if (_data is null)
                    {
                        var file = CacheFileName;
                        if (File.Exists(file))
                            try
                            {
                                _data = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(
                                    File.ReadAllText(file));
                            }
                            catch
                            {
                                File.Delete(file);
                            }

                        _data = _data ?? new Dictionary<string, HashSet<string>>();
                    }

                    if (_data.TryGetValue(nuspec.Item2.FullId, out var x))
                        return x;
                    var tmp = ScanDll(nuspec.Item1);
                    _hasNew = true;
                    return _data[nuspec.Item2.FullId] = tmp;
                }
            }

            public void FlushMe()
            {
                lock(l)
                {
                    if (_data is null || !_hasNew)
                        return;
                    var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                    var file = new FileInfo(CacheFileName);
                    file.Directory?.Create();
                    File.WriteAllText(file.FullName, json);
                }
            }

            private static string CacheFileName
            {
                get
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SolutionDoctor", "nugetDllCache.json");
                }
            }

            private readonly object l = new object();

            private Dictionary<string, HashSet<string>> _data;
            private bool _hasNew;
        }

        public static void Flush()
        {
            _cache.FlushMe();
        }


        private static readonly Cache _cache = new Cache();


        private static HashSet<string> ScanDll(string dirName)
        {
            var directoryInfo = new DirectoryInfo(dirName);
            var result        = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

        private IEnumerable<Problem> CheckProj(SolutionProject project)
        {
            var projectReferences = project.References.Where(a => a.HintPath != null).ToArray();
            if (!projectReferences.Any())
                yield break;
            foreach (var dep in projectReferences)
            {
                var nuspec = FindNuspecByFile(dep.HintPath);

                if (nuspec == null)
                {
                    var depName = GetDepDll(dep);
                    if (_excludeDll.Contains(depName))
                        continue;
                    if (_map.TryGetValue(depName, out var list) && list.Any())
                        yield return new AddNugetToSolutionProblem
                        {
                            ProjectFilename = project.Location,
                            Dependency      = dep,
                            SuggestedNugets = list
                        };
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
                    ProjectFilename    = project.Location,
                    PackageToReference = nuspec,
                    ReferencedLibrary  = dep,
                    IsCoreProject      = project.Kind == VsProjectKind.Core
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
                throw new ArgumentNullException(nameof(file));
            var hintToLower = file.FullName.ToLower();
            var query = from nuspec in _nuspecs
                where hintToLower.StartsWith(nuspec.Item1)
                select nuspec.Item2;
            var a = query.FirstOrDefault();
            if (a != null)
                return a;
            var d = file.Directory;
            for (var i = 0; i < 3; i++)
            {
                if (d == null) return null;
                if (string.Equals(d.Name, "packages", StringComparison.OrdinalIgnoreCase)) return null;
                try
                {
                    var p = d.GetFiles("*.nupkg");
                    if (p.Any())
                        return Nuspec.Load(p[0]);
                }
                catch
                {
                }

                d = d.Parent;
            }

            return null;
        }

        #endregion Methods

        #region Fields

        private Dictionary<string, HashSet<PackageId>> _map;
        private Tuple<string, Nuspec>[] _nuspecs;
        private List<SolutionProject> _projects;
        public HashSet<string> _excludeDll;

        #endregion Fields
    }
}