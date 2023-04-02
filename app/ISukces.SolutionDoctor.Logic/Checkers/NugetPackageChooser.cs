using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic.Checkers;

public class NugetPackageChooser
{
    private const string ns20 = "netstandard2.0";
    private static string FindFull(string version, string[] dirs)
    {
        var a = FindFull(version, dirs, a =>
        {
            var m = FullNetVersionRegex.Match(a);
            if (m.Success)
                return m.Groups[1].Value;
            return null;
        });
        if (a is not null)
            return a;
        if (version == "48")
        {
            if (dirs.Any(a => string.Equals(a, ns20, StringComparison.OrdinalIgnoreCase)))
                return ns20;
        }
        throw new NotSupportedException();
    }

    private static string FindFull(string version, string[] dirs, Func<string, string> getVer)
    {
        var candidates = dirs.Select(a =>
            {
                var m = getVer(a);
                return m is not null ? Tuple.Create(a, m) : null;
            }).Where(a => a is not null).OrderByDescending(a => a.Item2)
            .ToArray();
        var tmp            = candidates.Where(a => string.Compare(a.Item2, version, StringComparison.OrdinalIgnoreCase) <= 0).ToArray();
        var firstOrDefault = tmp.FirstOrDefault();
        return firstOrDefault?.Item1;
    }

    public static string FindTarget(string ver, string[] dirs)
    {
        if (dirs.Any(a => a == ver))
            return ver;
        {
            var m = FullNetVersionRegex.Match(ver);
            if (m.Success)
            {
                return FindFull(m.Groups[1].Value, dirs);
            }
        }
        var tmp =NetCoreFrameworkVersion.TryParse(ver);
        if (tmp is not null)
        {
            var possible = dirs.Select(NetCoreFrameworkVersion.TryParse).Where(a=>a is not null)
                .OrderByDescending(a=>a.CompareVersion)
                .ToArray();
            if ( tmp.IsPure)
            {
                var a = possible.Where(a => tmp.CompareVersion >= a.CompareVersion && a.IsPure).ToArray();
                if (a.Any())
                    return a.First().ToString();
                a = possible.Where(a => tmp.CompareVersion >= a.CompareVersion).ToArray();
                if (a.Any())
                    return a.First().ToString();
            }
            else
            {
                var a = possible.Where(a => tmp.CompareVersion >= a.CompareVersion && 
                                            (a.IsPure || tmp.Platform == a.Platform) ).ToArray();
                if (a.Any())
                    return a.First().ToString();
            }

            {
                var netstandard = dirs.Select(NetStandardFrameworkVersion.TryParse).Where(a => a is not null)
                    .OrderByDescending(a => a.CompareVersion)
                    .ToArray();
                
                var a = netstandard.Where(a => tmp.CompareVersion >= a.CompareVersion).ToArray();
                if (a.Any())
                    return a.First().ToString();
            }
        }
        throw new NotSupportedException();
    }

    #region Fields

    const string FullNetVersionFilter = @"^net(\d+)$";
    static readonly Regex FullNetVersionRegex = new Regex(FullNetVersionFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled);


    const string NetStandardFilter = @"^netstandard((?:\d+)\.(?:\d+))$";
    static Regex NetStandardRegex = new Regex(NetStandardFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion
}

