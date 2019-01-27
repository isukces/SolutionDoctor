using System;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class AssemblyBinding
    {
        public void SetRedirection([NotNull] string version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            var ns = XmlElement.Name.Namespace;
            var node = XmlElement.Element(ns + "bindingRedirect");
            var ver = version.ToString();
            node.SetAttributeValue("oldVersion", "0.0.0.0-" + ver);
            node.SetAttributeValue("newVersion", ver);
        }

        public void SetPackageId(string name)
        {
            Name = name;
            var ns   = XmlElement.Name.Namespace;
            var node = XmlElement.Element(ns + "assemblyIdentity");
            node.SetAttributeValue("name", name);
        }

        [CanBeNull]
        public static AssemblyBinding ParseDependentAssembly(XElement dependentAssemblyXElement)
        {
            var ns = dependentAssemblyXElement.Name.Namespace;
            var assemblyIdentity = dependentAssemblyXElement.Element(ns + "assemblyIdentity");
            if (assemblyIdentity == null)
                throw new NullReferenceException("assemblyIdentity");
            var bindingRedirect = dependentAssemblyXElement.Element(ns + "bindingRedirect");
            if (bindingRedirect == null)
                return null;
                // throw new NullReferenceException("bindingRedirect");
            return new AssemblyBinding
            {
                Name = (string)assemblyIdentity.Attribute("name"),
                OldVersion = (string)bindingRedirect.Attribute("oldVersion"),
                NewVersion = NugetVersion.Parse((string)bindingRedirect.Attribute("newVersion")),
                XmlElement = dependentAssemblyXElement
            };
        }


        public override string ToString()
        {
            return string.Format("{0} from {1} to {2}", Name, OldVersion, NewVersion);
        }


        public string Name { get; private set; }

        public string OldVersion { get; set; }

        public NugetVersion NewVersion { get; private set; }

        public XElement XmlElement { get; set; }
    }
}