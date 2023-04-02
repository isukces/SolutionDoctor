using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic.Checkers;

public class NetStandardFrameworkVersion
{
    public NetStandardFrameworkVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public static NetStandardFrameworkVersion TryParse(string version)
    {
        var m = NetStandardFrameworkVersionRegex.Match(version);
        if (!m.Success)
            return null;
        var major    = int.Parse(m.Groups[1].Value);
        var minor    = int.Parse(m.Groups[2].Value);
        var platform = m.Groups[3].Value;
        return new NetStandardFrameworkVersion(major, minor);
    }
    
    public int CompareVersion => Major * 100 + Minor;
     

    public int Major { get; }

    public int Minor { get; }

    public override string ToString()
    {
        return $"netstandard{Major}.{Minor}";
    }

    #region Fields

    static readonly Regex NetStandardFrameworkVersionRegex = new Regex(@"^netstandard(\d+)\.(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion
}
