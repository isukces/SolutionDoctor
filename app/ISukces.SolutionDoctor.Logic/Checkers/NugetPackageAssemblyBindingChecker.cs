using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using iSukces.Code.vssolutions;
using ISukces.SolutionDoctor.Logic.Problems;
using JetBrains.Annotations;


namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class NugetPackageAssemblyBindingChecker
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

        private static Func<FileInfo, bool> ContainsPackagePath(NugetPackage package)
        {
            var path = "\\packages\\" + package.Id + "." + package.Version + "\\";
            return a => a != null && a.FullName.ToLower().Contains(path.ToLowerInvariant());
        }

        public class InstanceProxy : MarshalByRefObject
        {
            public void LoadAssembly(string path)
            {
                 AppDomain.CurrentDomain.AssemblyResolve += (a, b) =>
                 {
                     return null;
                 };
                Assembly asm   = Assembly.LoadFile(path);
                Type[] types = asm.GetExportedTypes();
                var ver1 = asm.GetName();
                var ver2 = ver1.Version;
                Console.WriteLine(ver1);
                
                string assemblyVersion = asm.GetName().Version.ToString(); 
                //string assemblyVersion = Assembly.LoadFile("your assembly file").GetName().Version.ToString(); 
                string fileVersion     = FileVersionInfo.GetVersionInfo(asm.Location).FileVersion; 
                string productVersion  = FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;

                string fileVersion2    = FileVersionInfo.GetVersionInfo(path).FileVersion; 
                string productVersion2 = FileVersionInfo.GetVersionInfo(path).ProductVersion;
                // ...see above...
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

                var currentAssemblyName = AssemblyName.GetAssemblyName(file.FullName);
                var version = currentAssemblyName.Version; 
                var compression = @"packages\System.IO.Compression.4.3.0\lib\net46\System.IO.Compression.dll";
                if (file.FullName.ToLower().EndsWith(compression.ToLower()))
                {
                    version = Version.Parse("4.2.0.0");
                }


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
                {
                    if (_removeBindingRedirect.Contains(i.Name))
                        yield return new NotNecessaryOrForceVersionBindingRedirectProblem
                        {
                            Redirect        = i,
                            ProjectFilename = project.Location
                        };
                    else if (_forceBindingRedirects.TryGetValue(i.Name, out var version))
                    {
                        if (i.NewVersion != version)
                            yield return new NotNecessaryOrForceVersionBindingRedirectProblem
                            {
                                Redirect        = i,
                                ProjectFilename = project.Location,
                                Version         = version
                            };
                    }
                }

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

                dlls = dlls.Where(a =>
                {
                    var name = a.GetShortNameWithoutExtension();
                    if (_removeBindingRedirect.Contains(name))
                        return false;
                    if (_forceBindingRedirects.ContainsKey(name))
                        return false;
                    return true;
                }).ToArray();
                if (dlls.Length == 0)
                {
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

        private HashSet<string> _removeBindingRedirect;
        public Dictionary<string, NugetVersion> _forceBindingRedirects;

        private class DllInfo
        {
            public DllInfo(FileInfo file, string dllVersion, bool exists)
            {
                File       = file;
                DllVersion = dllVersion;
                Exists     = exists;
            }

            public FileInfo File       { get; }
            public string   DllVersion { get; }
            public bool     Exists     { get; }
        }
    }
}