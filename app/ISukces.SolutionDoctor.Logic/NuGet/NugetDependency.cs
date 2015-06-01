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
            return string.Format("NugetDependency {0} {1}", Id, Version);
        }

        public static NugetDependency FromNode(XElement x)
        {
            return new NugetDependency
            {
                Id = (string)x.Attribute("id"),
                Version = NugetVersion.Parse((string)x.Attribute("version"))
            };
        }

        #endregion Static Methods

        #region Properties

        public string Id { get; private set; }

        public NugetVersion Version { get; private set; }

        #endregion Properties
    }
}