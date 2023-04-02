using System.IO;

namespace ISukces.SolutionDoctor
{
    public class DebugConfigs
    {
        public static string[] SetArgs(string[] args)
        {
            string[] Make(string dir, string cfg)
            {
                Directory.SetCurrentDirectory(dir);
                return new[]
                {
                    ".\\", "-saveOptions", cfg, "-cfg", cfg
                };
            }

            if (args.Length != 1) return args;
            switch (args[0])
            {
                case "isukcesserenity": return Make(@"C:\programs\isukces\isukces.Serenity", "solutionDoctor.json");
                case "isukcesbase": return Make(@"C:\programs\isukces\dotnetLib\isukces.Base\src", "solutionDoctor.json");
                case "pd":
                    return Make(@"C:\programs\ALPEX\PipelineDesigner", "app\\solutionDoctor.json");
                case "ct":
                    Directory.SetCurrentDirectory(@"C:\programs\conexx");
                    // Directory.SetCurrentDirectory(@"C:\programs\conexx\conexx.total\app\_tests_\Conexx.FinishingPlates.Tests");
                    return new[]
                    {
                        "-runExternalFix", "-NoWarn", "1591,1573,618", "-WarningsAsErrors",
                        "108,414,162,168,169,219,628,649,693,1570,1587,1572,1574,1718,1734", "-cfg",
                        @"C:\programs\conexx\SolutionDoctor.json"
                    };
                case "home":
                    Directory.SetCurrentDirectory(@"C:\programs\ARDUINO");
                    return new[]
                    {
                        ".\\", "-cfg", "solutionDoctor.json"
                    };
                // Directory.SetCurrentDirectory(@"C:\programs\conexx");
                default: return args;
            }
        }
    }
}
