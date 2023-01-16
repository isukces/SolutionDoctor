using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using isukces.code.vssolutions;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    public class NugetPackageAssemblyBindingChecker
    {
        public static IEnumerable<Problem> Check([NotNull] IList<SolutionProject> projects,
            HashSet<string> removeBindingRedirect, Dictionary<string, string> forceBindingRedirects)
        {
            if (projects == null) throw new ArgumentNullException(nameof(projects));
            var tmp = new NugetPackageAssemblyBindingChecker
            {
                _removeBindingRedirect = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                _forceBindingRedirects = new Dictionary<string, NugetVersion>(StringComparer.OrdinalIgnoreCase)
            };
            if (removeBindingRedirect != null)
                foreach (var i in removeBindingRedirect)
                    tmp._removeBindingRedirect.Add(i);
            if (forceBindingRedirects != null)
                foreach (var i in forceBindingRedirects)
                    tmp._forceBindingRedirects[i.Key] = NugetVersion.Parse(i.Value);
            return projects.SelectMany(tmp.ScanProject).ToList();
        }

        private static FileInfo[] FindDlls(SolutionProject project, NugetPackage package)
        {
            var dlls =
                project.References.Select(a => a.HintPath)
                    .Where(DllVersionRepo.ContainsPackagePath(package))
                    .ToArray();
            var path = Path.Combine(HardCoded.Cache, package.Id, package.Version.ToString(), "lib");
            if (Directory.Exists(path))
            {
                var sink = new List<FileInfo>();
                DllVersionRepo.Can(path, sink);
                sink.AddRange(dlls);
                dlls = sink.ToArray();
            }

            if (dlls.Length < 2)
                return dlls;
            var t = project.TargetFrameworkVersion;
            if (string.IsNullOrEmpty(t))
                return dlls;

            var projectFrameworkVersion = FrameworkVersion.Parse(t).Single();
            if (projectFrameworkVersion is null)
                return dlls;
            var tmp = dlls.Select(fileInfo => DllVersionRepo.FindPossibleOrNull(fileInfo, projectFrameworkVersion))
                .Where(a => a != null)
                .OrderBy(a => a.Loading)
                .GroupBy(a => a.File.Name)
                .ToDictionary(a => a.Key, a => a.ToArray())
                .ToArray();
            if (tmp.Length == 1)
            {
                var values = tmp.Single().Value;
                if (values.Length == 1)
                    return new[] { values.Single().File.GetFileInfo() };
                return new[] { values.Last().File.GetFileInfo() };
            }

            return dlls;
        }


        IEnumerable<Problem> Decisions(FileInfo[] dlls, NugetPackage package, SolutionProject project, AssemblyBinding redirect)
        {
            var dlls2 = dlls
                .GroupBy(a => a.Name, FileName.FileNameComparer)
                .ToDictionary(a => a.Key, a => a.ToArray());

            bool Check()
            {
                foreach (var i in dlls2.Keys)
                {
                    var n  = new FileInfo(i);
                    var nn = i.Substring(0, i.Length - n.Extension.Length) + ".resources" + n.Extension;
                    if (dlls2.ContainsKey(nn))
                    {
                        dlls2.Remove(nn);
                        return true;
                    }
                }

                return false;
            }

            bool next = true;
            while (next && dlls2.Count > 1)
            {
                next = Check();
            }

            bool dllsOk = false;

            if (dlls2.Count > 1)
            {
                var expected = redirect.Name + ".dll";
                if (dlls2.TryGetValue(expected, out var f))
                {
                    dlls2.Clear();
                    dlls2[expected] = f;
                    dlls            = f;
                    dllsOk          = true;
                }
                
            }

            if (dlls2.Count == 0)
            {
                yield return new UnableToGetReferencedDllVersionProblem(package.Id, project, "no hint path to dll");
                yield break;
            }

            

            if (dlls2.Count > 1)
            {
                yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                    $"Too many hint paths to dll for {package.Id} ver {package.Version} package. Probably package contains more than one dll inside.");
                yield break;
            }

            if (!dllsOk)
            {
                dlls = dlls.Where(a =>
                {
                    var name = a.GetShortNameWithoutExtension();
                    if (_removeBindingRedirect.Contains(name))
                        return false;
                    if (_forceBindingRedirects.ContainsKey(name))
                        return false;
                    return true;
                }).ToArray();
            }

            if (dlls.Length == 0) yield break;

            var versions = dlls.Select(DllVersionRepo.GetDllInfo).Distinct().ToArray();
            {
                var aa = versions.Where(q => q.DllVersion == null || !q.Exists).ToArray();
                if (aa.Any())
                {
                    foreach (var aaa in aa)
                        yield return
                            new UnableToGetReferencedDllVersionProblem(package.Id, project,
                                "Broken file " + aaa?.File);
                    yield break;
                }
            }
            var vers2 = versions.Select(a => a.DllVersion).Distinct().ToArray();
            if (vers2.Length != 1)
            {
                yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                    "Too many possible versions to compare");
                yield break;
            }

            if (vers2.Any(a => a == redirect.NewVersion.NormalizedVersion.ToString())) yield break;

            yield return new WrongBindingRedirectProblem
            {
                ProjectFilename = project.Location,
                Redirect        = redirect,
                Package         = package,
                DllVersion      = versions[0].DllVersion
            };
        }

        private IEnumerable<Problem> ScanProject(SolutionProject project)
        {
            if (string.Equals(project.Location.FullName, @"c:\programs\conexx\conexx.total\app\_tests_\Conexx.FinishingPlates.Tests\Conexx.FinishingPlates.Tests.csproj",
                    StringComparison.OrdinalIgnoreCase))
                Debug.Write("");
            var assemblyBindings = project.AssemblyBindings;
            var packageVersion   = project.NugetPackages;

            if (!assemblyBindings.Any()) yield break;
            if (_removeBindingRedirect != null && _removeBindingRedirect.Any())
                foreach (var i in assemblyBindings)
                    if (_removeBindingRedirect.Contains(i.Name))
                        yield return new NotNecessaryOrForceVersionBindingRedirectProblem
                        {
                            Redirect        = i,
                            ProjectFilename = project.Location
                        };
                    else if (_forceBindingRedirects.TryGetValue(i.Name, out var version))
                        if (i.NewVersion != version)
                            yield return new NotNecessaryOrForceVersionBindingRedirectProblem
                            {
                                Redirect        = i,
                                ProjectFilename = project.Location,
                                Version         = version
                            };

            if (!packageVersion.Any()) yield break;
            foreach (var package in packageVersion)
            {
                if (project.Kind == VsProjectKind.Core)
                {
                    var redirects = assemblyBindings.Where(
                            a => string.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    foreach (var redirect1 in redirects)
                    {
                        if (string.Equals(redirect1.Name, package.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            var dlls1 = _bla.GetDlls(package, project.TargetFrameworkVersion);
                            var props = Decisions(dlls1, package, project, redirect1).ToArray();
                            foreach (var i in props)
                                yield return i;
                        }
                    }

                    continue;
                }

                {
                    var redirect =
                        assemblyBindings.FirstOrDefault(
                            a => string.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase));
                    if (redirect == null) continue;
                    var dlls = FindDlls(project, package);

                    var props = Decisions(dlls, package, project, redirect).ToArray();
                    foreach (var i in props)
                        yield return i;
                }
            }
        }

        #region Fields

        private readonly DllVersionRepo _bla = new DllVersionRepo();

        private HashSet<string> _removeBindingRedirect;
        public Dictionary<string, NugetVersion> _forceBindingRedirects;

        #endregion

        [ImmutableObject(true)]
        public class Result
        {
            public Result(PossibleToLoadNuget loading, FileName file)
            {
                Loading = loading;
                File    = file;
            }

            public override string ToString()
            {
                return $"Loading={Loading}, File={File}";
            }

            #region properties

            public PossibleToLoadNuget Loading { get; }

            public FileName File { get; }

            #endregion
        }

        public class InstanceProxy : MarshalByRefObject
        {
            public void LoadAssembly(string path)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (a, b) => { return null; };
                var asm   = Assembly.LoadFile(path);
                var types = asm.GetExportedTypes();
                var ver1  = asm.GetName();
                var ver2  = ver1.Version;
                Console.WriteLine(ver1);

                var assemblyVersion = asm.GetName().Version.ToString();
                //string assemblyVersion = Assembly.LoadFile("your assembly file").GetName().Version.ToString(); 
                var fileVersion    = FileVersionInfo.GetVersionInfo(asm.Location).FileVersion;
                var productVersion = FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;

                var fileVersion2    = FileVersionInfo.GetVersionInfo(path).FileVersion;
                var productVersion2 = FileVersionInfo.GetVersionInfo(path).ProductVersion;
                // ...see above...
            }
        }
    }

    internal class DllInfo
    {
        public DllInfo(FileInfo file, string dllVersion, bool exists)
        {
            File       = file;
            DllVersion = dllVersion;
            Exists     = exists;
        }

        #region properties

        public FileInfo File       { get; }
        public string   DllVersion { get; }
        public bool     Exists     { get; }

        #endregion
    }
}
