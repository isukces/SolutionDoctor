using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class Project
    {
        #region Static Methods

        // Private Methods 


        #endregion Static Methods

        #region Methods

        // Public Methods 

        public List<AssemblyBinding> AssemblyBindings
        {
            get
            {
                return _assemblyBindings ?? (_assemblyBindings = AssemblyBindingsInternal().ToList());
            }
        }

        private IEnumerable<AssemblyBinding> AssemblyBindingsInternal()
        {
            var configFileInfo = File.GetAppConfigFile();
            var cfg = new AppConfig(configFileInfo);

            return cfg.GetAssemblyBindings();
        }

        public NugetPackage[] NugetPackages
        {
            get
            {
                return _nugetPackages ?? (_nugetPackages = NugetPackagesInternal());
            }
        }

        public override string ToString()
        {
            return String.Format("{0} at {1}", Name, File.FullName);
        }
        // Private Methods 

        private NugetPackage[] NugetPackagesInternal()
        {
            // ReSharper disable once PossibleNullReferenceException
            var configFileInfo = File.GetPackagesConfigFile();
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

        private NugetPackage[] _nugetPackages;
        private List<AssemblyBinding> _assemblyBindings;

        #endregion Fields

        #region Properties

        public string Name { get; set; }

        public Guid LocationUid { get; set; }

        public FileInfo File { get; set; }

        public Guid ProjectUid { get; set; }

        #endregion Properties
    }
}