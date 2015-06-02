using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public sealed class Project : IEquatable<Project>
    {
		#region Static Methods 

		// Public Methods 

        public static bool operator !=(Project left, Project right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(Project left, Project right)
        {
            return Equals(left, right);
        }

		#endregion Static Methods 

		#region Methods 

		// Public Methods 

        public bool Equals(Project other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other)
                || Equals(Location, other.Location);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Project)obj);
        }

        public override int GetHashCode()
        {
            var tmp = Location;
            return (tmp != null ? tmp.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return string.Format("Project {0}", Location);
            // return String.Format("{0} at {1}", Name, File.Name);
        }
		// Private Methods 

        private IEnumerable<AssemblyBinding> AssemblyBindingsInternal()
        {
            var configFileInfo = Location.GetAppConfigFile();
            var cfg = new AppConfig(configFileInfo);

            return cfg.GetAssemblyBindings();
        }

        private List<ProjectReference> GetReferences()
        {
            var xml = XDocument.Load(Location.FullName);
            var root = xml.Root;
            var result = new List<ProjectReference>();
            if (root == null) return result;
            foreach (var itemGroupElement in root.Elements(root.Name.Namespace + "ItemGroup"))
                foreach (var reference in itemGroupElement.Elements(itemGroupElement.Name.Namespace + "Reference"))
                    result.Add(ProjectReference.FromNode(reference, Location.Directory));
            return result;
        }

        private NugetPackage[] NugetPackagesInternal()
        {
            // ReSharper disable once PossibleNullReferenceException
            var configFileInfo = Location.GetPackagesConfigFile();
            if (!configFileInfo.Exists)
                return new NugetPackage[0];
            var xml = XDocument.Load(configFileInfo.FullName);
            var root = xml.Root;
            if (root == null || root.Name.LocalName != "packages")
                return new NugetPackage[0];
            var packages = root.Elements(root.Name.Namespace + "package");

            return packages.Select(NugetPackage.Parse).ToArray();
        }

		#endregion Methods 

		#region Fields 

        private List<AssemblyBinding> _assemblyBindings;
        private NugetPackage[] _nugetPackages;
        List<ProjectReference> _references;

		#endregion Fields 

		#region Properties 

        public List<AssemblyBinding> AssemblyBindings
        {
            get
            {
                return _assemblyBindings ?? (_assemblyBindings = AssemblyBindingsInternal().ToList());
            }
        }

        public NugetPackage[] NugetPackages
        {
            get
            {
                return _nugetPackages ?? (_nugetPackages = NugetPackagesInternal());
            }
        }

        public List<ProjectReference> References
        {
            get { return _references ?? (_references = GetReferences()); }
        }

        public FileName Location { get; set; }

		#endregion Properties 


        // public string Name { get; set; }
        // public Guid LocationUid { get; set; }
        // public Guid ProjectUid { get; set; }
    }

    
}