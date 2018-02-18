using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class NugetPackage
    {
        #region Static Methods

        // Public Methods 

        public static NugetPackage Parse(XElement packageXElement)
        {
            var ver = (string)packageXElement.Attribute("version");
            NugetVersion parsed = NugetVersion.Parse(ver);
           // if (!Version.TryParse(ver, out parsed))
             //   throw new Exception("Invalid version " + ver);
            return new NugetPackage
            {
                Id = (string)packageXElement.Attribute("id"),
                Version = parsed,
                TargetFramework = (string)packageXElement.Attribute("targetFramework")
            };
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Id, Version, TargetFramework);
        }

        #endregion Methods

        #region Properties

        public string TargetFramework { get; set; }

        public NugetVersion Version { get; set; }

        public string Id { get; set; }

        #endregion Properties
    }
}