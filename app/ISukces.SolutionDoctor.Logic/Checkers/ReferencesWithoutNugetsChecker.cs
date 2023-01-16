using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class ReferencesWithoutNugetsChecker
    {
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

        public static void Flush()
        {
            _cache.FlushMe();
        }

        private static string GetDepDll(ProjectReference dep)
        {
            if (dep.HintPath == null)
                return null;
            return dep.HintPath.Name;
        }


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
                var hitedDllNotFound = !dep.HintPath.Exists;
                var nuspec           = FindNuspecByFile(dep.HintPath);

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
                            SuggestedNugets = list,
                            ExtraInfo       = hitedDllNotFound ? $"File {dep.HintPath.FullName} doesn't exists" : ""
                        };
                    continue;
                }

                var candidates = project.NugetPackages
                    .Where(a => string.Equals(a.Id, nuspec.Id, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var nugetPackage = candidates
                    .FirstOrDefault(a => a.Version.NormalizedVersion == nuspec.PackageVersion.NormalizedVersion);
                if (nugetPackage != null) continue;
                var problem = new NuGetPackageShouldBeReferencedProblem
                {
                    ProjectFilename    = project.Location,
                    PackageToReference = nuspec,
                    ReferencedLibrary  = dep,
                    IsCoreProject      = project.Kind == VsProjectKind.Core
                };
                if (candidates.Any())
                {
                    problem.VersionProblem = "contains " + string.Join(",", candidates.Select(a => a.Version)) + " but needs " + nuspec.PackageVersion;
                }

                yield return problem;
            }
        }

        private Nuspec FindNuspecByFile([NotNull] FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            var hintToLower = file.FullName.ToLower();
            var query = from nuspec in _nuspecs
                where hintToLower.StartsWith(nuspec.Item1, StringComparison.OrdinalIgnoreCase)
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

        #region Fields

        private static readonly Cache _cache = new Cache();

        private Dictionary<string, HashSet<PackageId>> _map;
        private Tuple<string, Nuspec>[] _nuspecs;
        private List<SolutionProject> _projects;
        public HashSet<string> _excludeDll;

        #endregion

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

            #region properties

            private static string CacheFileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SolutionDoctor", "nugetDllCache.json");

            #endregion

            #region Fields

            private readonly object l = new object();

            private Dictionary<string, HashSet<string>> _data;
            private bool _hasNew;

            #endregion
        }
    }
}
