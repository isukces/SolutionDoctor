using System.Reflection;
using isukces.code.vssolutions;
using iSukces.Code.VsSolutions;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic.Checkers;

public class DllVersionRepo
{
    public DllVersionRepo()
    {
        var up = Environment.GetEnvironmentVariable("USERPROFILE");
        _git = Path.Combine(up, ".nuget", "packages");
    }

    public static void Can(string path, List<FileInfo> sink)
    {
        var di = new DirectoryInfo(path);
        if (!di.Exists)
            return;
        sink.AddRange(di.GetFiles("*.dll"));
        foreach (var i in di.GetDirectories()) Can(i.FullName, sink);
    }

    public static Func<FileInfo, bool> ContainsPackagePath(NugetPackage package)
    {
        var path = "\\packages\\" + package.Id + "." + package.Version + "\\";
        return a => a != null && a.FullName.ToLower().Contains(path.ToLowerInvariant());
    }


    private static FileInfo[] FindDlls(string path, string projectTarget)
    {
        if (!string.IsNullOrEmpty(projectTarget))
        {
            var dirs   = new DirectoryInfo(path).GetDirectories().Select(a => a.Name).ToArray();
            var target = NugetPackageChooser.FindTarget(projectTarget, dirs);
            path = Path.Combine(path, target);
        }

        /*var dlls =
            project.References.Select(a => a.HintPath)
                .Where(ContainsPackagePath(package))
                .ToArray();*/
        var dlls = Array.Empty<FileInfo>();

        if (Directory.Exists(path))
        {
            var sink = new List<FileInfo>();
            Can(path, sink);
            sink.AddRange(dlls);
            dlls = sink.ToArray();
        }

        if (dlls.Length < 2)
            return dlls;
        /*var t = project.TargetFrameworkVersion;
        
        if (string.IsNullOrEmpty(t))*/
        return dlls;

        /*
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

        return dlls;*/
    }

    public static NugetPackageAssemblyBindingChecker.Result FindPossibleOrNull(FileInfo fileInfo, FrameworkVersion frameworkVersion)
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
                var tmp = frameworkVersion.CanLoad(q);
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
                return new NugetPackageAssemblyBindingChecker.Result(possible[0], new FileName(fileInfo));
            default:
                return new NugetPackageAssemblyBindingChecker.Result(possible.Last(), new FileName(fileInfo));
        }
    }

    internal static DllInfo GetDllInfo(FileInfo file)
    {
        if (!file.Exists)
            return new DllInfo(file, null, false);
        try
        {
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


    /*
    public bool TryGetAssemblyVersion(NugetPackage package, out NugetVersion[] o)
    {
        o = default;
        List<CacheItem> cacheItems = new List<CacheItem>();
        lock(l)
        {
            var path1     = Path.Combine(_git, package.Id);
            var cacheFile = Path.Combine(path1, "version.cache");
            if (File.Exists(cacheFile))
            {
                cacheItems = JsonConvert.DeserializeObject<List<CacheItem>>(File.ReadAllText(cacheFile)) ?? cacheItems;
                var q = cacheItems.FirstOrDefault(a => a.Version == package.Version.ToString());
                if (q is not null)
                {
                    if (q.HasValue)

                        o = q.DllVersion;
                    return q.HasValue;
                }
            }

            var path = Path.Combine(_git, package.Id, package.Version.ToString(), "lib");
            var dlls = FindDlls(path);

            var versions = dlls.Select(GetDllInfo).ToArray();
            var tmp      = versions.Select(a => a.DllVersion).Distinct().ToArray();
            o = tmp.Select(NugetVersion.Parse).ToArray();
            return true;

        }
    }
    */

    #region Fields

    private static readonly object l = new();
    private readonly string _git;

    #endregion

    class CacheItem
    {
        #region properties

        public string         Version    { get; set; }
        public NugetVersion[] DllVersion { get; set; }
        public bool           HasValue   { get; set; }

        #endregion
    }

    public FileInfo[] GetDlls(NugetPackage package, string projectTarget)
    {
        string path = Path.Combine(_git, package.Id, package.Version.ToString(), "lib");
        return FindDlls(path, projectTarget);
    }
}