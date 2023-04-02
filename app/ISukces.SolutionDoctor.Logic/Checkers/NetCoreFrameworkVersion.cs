using System.Text.RegularExpressions;

namespace ISukces.SolutionDoctor.Logic.Checkers;

public class NetCoreFrameworkVersion
{
    public NetCoreFrameworkVersion(string version)
    {
        var m = NetCoreFrameworkVersionRegex.Match(version);
        if (!m.Success)
            throw new NotSupportedException();
        Major    = int.Parse(m.Groups[1].Value);
        Minor    = int.Parse(m.Groups[2].Value);
        Platform = m.Groups[3].Value;
    }

    public int Major { get; }

    public int Minor { get; }

    public string Platform { get; }

    public override string ToString()
    {
        return $"{Major}.{Minor}-{Platform}";
    }

    #region Fields

    // ^net(\d+)\.(\d)(?:-(.*))?$
    // ^net(\d+)\.(\d+)(?:-(.*))?$
    static readonly Regex NetCoreFrameworkVersionRegex = new Regex(@"^net(\d+)\.(\d+)(?:-(.*))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion
}
