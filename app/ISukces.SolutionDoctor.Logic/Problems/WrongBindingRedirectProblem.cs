using System;
using System.IO;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class WrongBindingRedirectProblem : Problem
    {
        #region Constructors



        #endregion Constructors

        #region Methods

        // Public Methods 

        public override void Describe(Action<string> writeLine)
        {
            writeLine(string.Format("{2} nuget package is {0} but config redirects to {1}",
                Package.Version,
                Redirect.NewVersion,
                Package.Id));
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix(
                string.Format("Set redirection to {0} for {1} package in project {2}", 
                    Package.Version.Version, 
                    Package.Id,
                    new FileInfo(ProjectFilename).Name),
                FixMethod);
        }

        void FixMethod()
        {
            var fn = new FileInfo(ProjectFilename).GetAppConfigFile();
            var xml = new AppConfig(fn);
            if (!xml.Exists)
                throw new Exception(string.Format("Config file {0} doesn't exist", fn.FullName));
            var node = xml.FindByAssemblyIdentity(Package.Id);
            if (node == null)
                throw new Exception(string.Format("Redirection for '{0}' not found", Package.Id));
            node.SetRedirection(Package.Version.Version);
            xml.Save();
        }
        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #endregion Methods

        #region Properties

        public string ConfigFile
        {
            get
            {
                return new FileInfo(ProjectFilename).GetPackagesConfigFile().FullName;
            }
        }

        public AssemblyBinding Redirect { get; set; }

        public NugetPackage Package { get; set; }

        #endregion Properties
    }
}