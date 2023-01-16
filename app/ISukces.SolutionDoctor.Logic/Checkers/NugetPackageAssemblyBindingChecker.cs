using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using isukces.code.vssolutions;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class NugetPackageAssemblyBindingChecker
    {
        private static void Can(string path, List<FileInfo> sink)
        {
            var di = new DirectoryInfo(path);
            if (!di.Exists)
                return;
            sink.AddRange(di.GetFiles("*.dll"));
            foreach (var i in di.GetDirectories())
                Can(i.FullName, sink);
        }

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

        private static Func<FileInfo, bool> ContainsPackagePath(NugetPackage package)
        {
            var path = "\\packages\\" + package.Id + "." + package.Version + "\\";
            return a => a != null && a.FullName.ToLower().Contains(path.ToLowerInvariant());
        }

        private static FileInfo[] FindDlls(SolutionProject project, NugetPackage package)
        {
            var dlls =
                project.References.Select(a => a.HintPath)
                    .Where(ContainsPackagePath(package))
                    .ToArray();
            var path = Path.Combine(HardCoded.Cache, package.Id, package.Version.ToString(), "lib");
            if (Directory.Exists(path))
            {
                var sink = new List<FileInfo>();
                Can(path, sink);
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
            var tmp = dlls.Select(fileInfo => FindPossibleOrNull(fileInfo, projectFrameworkVersion))
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

        public static Result FindPossibleOrNull(FileInfo fileInfo, FrameworkVersion project)
        {
            string dirShortName = null;
            var    d            = fileInfo.Directory;
            var    list         = new List<string>();
            while (d != null)
            {
                if (d.Name == "lib")
                {
                    dirShortName = list.LastOrDefault();
                }

                list.Add(d.Name);
                d = d.Parent;
            }

            // var dirShortName = fileInfo.Directory?.Name;
            if (string.IsNullOrEmpty(dirShortName))
                return null;

            var nugetVersions = FrameworkVersion.Parse(dirShortName);
            if (nugetVersions is null || nugetVersions.Any(version => version == null))
                return null;
            // throw new NotSupportedException();

            var possible = nugetVersions
                .Select(q =>
                {
                    var tmp = project.CanLoad(q);
                    if (tmp == NugetLoadCompatibility.None)
                        return null;
                    return new PossibleToLoadNuget(q, tmp);
                })
                .Where(a => a != null)
                .OrderBy(a => a)
                .ToArray();

            switch (possible.Length)
            {
                case 0:
                    return null;
                case 1:
                    return new Result(possible[0], new FileName(fileInfo));
                default:
                    return new Result(possible.Last(), new FileName(fileInfo));
            }
        }

        private static DllInfo GetDllInfo(FileInfo file)
        {
            if (!file.Exists)
                return new DllInfo(file, null, false);
            try
            {
                /*if (file.FullName.ToLower().Contains("system.io.comp"))
                {
                    Debug.Write("");

                    {
                        AppDomain domain = AppDomain.CreateDomain("TempDomain");
                        InstanceProxy proxy = domain.CreateInstanceAndUnwrap(Assembly.GetAssembly(
                            typeof(InstanceProxy)).FullName, typeof(InstanceProxy).ToString()) as InstanceProxy;
                        if (proxy != null)
                        {
                            proxy.LoadAssembly(file.FullName);
                        }
                        AppDomain.Unload(domain);
                    }
                }*/

                var currentAssemblyName                                              = AssemblyName.GetAssemblyName(file.FullName);
                var version                                                          = currentAssemblyName.Version;
                var compression                                                      = @"packages\System.IO.Compression.4.3.0\lib\net46\System.IO.Compression.dll";
                if (file.FullName.ToLower().EndsWith(compression.ToLower())) version = Version.Parse("4.2.0.0");

                return new DllInfo(file, version.ToString(), true);
            }
            catch
            {
                return new DllInfo(file, null, true);
            }
        }

        private IEnumerable<Problem> ScanProject(SolutionProject project)
        {
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
                    foreach (var i in redirects)
                        if (i.Name != package.Id)
                            yield return new AssemblyRedirectionInvalidPackageId(package.Id, project);

                    continue;
                }

                var redirect =
                    assemblyBindings.FirstOrDefault(
                        a => string.Equals(a.Name, package.Id, StringComparison.OrdinalIgnoreCase));
                if (redirect == null) continue;
                var dlls = FindDlls(project, package);
                var dlls2 = dlls
                    .GroupBy(a => a.Name, FileName.FileNameComparer)
                    .ToDictionary(a => a.Key, a => a.ToArray());
                {
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
                }
                if (dlls2.Count == 0)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project, "no hint path to dll");
                    continue;
                }

                if (dlls2.Count > 1)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                        $"Too many hint paths to dll for {package.Id} ver {package.Version} package. Probably package contains more than one dll inside.");
                    continue;
                }

                dlls = dlls.Where(a =>
                {
                    var name = a.GetShortNameWithoutExtension();
                    if (_removeBindingRedirect.Contains(name))
                        return false;
                    if (_forceBindingRedirects.ContainsKey(name))
                        return false;
                    return true;
                }).ToArray();
                if (dlls.Length == 0) continue;

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
                var vers2 = versions.Select(a => a.DllVersion).Distinct().ToArray();
                if (vers2.Length != 1)
                {
                    yield return new UnableToGetReferencedDllVersionProblem(package.Id, project,
                        "Too many possible versions to compare");
                    continue;
                }

                if (vers2.Any(a => a == redirect.NewVersion.NormalizedVersion.ToString())) continue;

                yield return new WrongBindingRedirectProblem
                {
                    ProjectFilename = project.Location,
                    Redirect        = redirect,
                    Package         = package,
                    DllVersion      = versions[0].DllVersion
                };
            }
        }

        #region Fields

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


        private class DllInfo
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
}
