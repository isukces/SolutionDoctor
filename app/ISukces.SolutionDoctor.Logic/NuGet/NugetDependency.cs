using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class NugetDependency
    {
        #region Static Methods

        // Public Methods 
        public override string ToString()
        {
            return string.Format("NugetDependency {0} {1}", Id, Versions);
        }

        public static NugetDependency FromNode(XElement x)
        {
            var ver = (string)x.Attribute("version");
            return new NugetDependency
            {
                Id = (string)x.Attribute("id"),
                Versions = string.IsNullOrEmpty(ver) ? NugetVersionRange.Any : NugetVersionRange.Parse(ver)
            };
        }

        #endregion Static Methods

        #region Properties

        public string Id { get; private set; }

        public NugetVersionRange Versions { get; private set; }

        #endregion Properties
    }
}