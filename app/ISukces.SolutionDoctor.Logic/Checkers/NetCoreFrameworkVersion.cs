using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic.Checkers;

// class that contains net core framework version including major and minor version and target platform
public class NetCoreFrameworkVersion
{
    public NetCoreFrameworkVersion(int major, int minor, string platform)
    {
        Major    = major;
        Minor    = minor;
        Platform = platform;
    }

    public static NetCoreFrameworkVersion TryParse(string version)
    {
        var m = NetCoreFrameworkVersionRegex.Match(version);
        if (!m.Success)
            return null;
        var major    = int.Parse(m.Groups[1].Value);
        var minor    = int.Parse(m.Groups[2].Value);
        var platform = m.Groups[3].Value;
        return new NetCoreFrameworkVersion(major, minor, platform);
    }
    
    public int CompareVersion => Major * 100 + Minor;
    
    public bool IsPure => string.IsNullOrEmpty(Platform);

    public int Major { get; }

    public int Minor { get; }

    public string Platform { get; }

    public override string ToString()
    {
        if (IsPure)
        return $"net{Major}.{Minor}";
        return $"net{Major}.{Minor}-{Platform}";
    }

    #region Fields

    static readonly Regex NetCoreFrameworkVersionRegex = new Regex(@"^net(\d+)\.(\d+)(?:-(.*))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion
}

// class that contains net core framework version including major and minor version and target platform