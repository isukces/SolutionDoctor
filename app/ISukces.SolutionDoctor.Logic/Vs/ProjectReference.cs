using System.IO;
using System.Xml.Linq;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class ProjectReference
    {
        #region Static Methods

        // Public Methods 

        public static ProjectReference FromNode(XElement reference, DirectoryInfo baseDir)
        {
            var hintPathElement = reference.Element(reference.Name.Namespace + "HintPath");
            var hintPath        = hintPathElement == null ? null : hintPathElement.Value;
            return new ProjectReference
            {
                Name = (string)reference.Attribute("Include"),
                HintPath = string.IsNullOrEmpty(hintPath)
                    ? null
                    : new FileInfo(Path.Combine(baseDir.FullName, hintPath))
            };
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public override string ToString()
        {
            return Name;
        }

        #endregion Methods

        #region Properties

        public FileInfo HintPath { get; set; }

        public string Name { get; set; }

        #endregion Properties
    }
}