using System;
using System.IO;
using ISukces.SolutionDoctor.Logic.NuGet;
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
            writeLine(string.Format("{2} nuget package DLL is {0} but config redirects to {1}",
                DllVersion,
                Redirect.NewVersion,
                Package.Id));
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix(
                string.Format("Set redirection to {0} for {1} package in project {2}",
                    DllVersion, 
                    Package.Id,
                    ProjectFilename.Name),
                FixMethod);
        }

        void FixMethod()
        {
            var fn = ProjectFilename.GetAppConfigFile();
            var xml = new AppConfig(fn);
            if (!xml.Exists)
                throw new Exception(string.Format("Config file {0} doesn't exist", fn.FullName));
            var node = xml.FindByAssemblyIdentity(Package.Id);
            if (node == null)
                throw new Exception(string.Format("Redirection for '{0}' not found", Package.Id));
            node.SetRedirection(DllVersion);
            xml.Save();
        }
        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #endregion Methods

        #region Properties

        public FileName ConfigFile
        {
            get
            {
                return ProjectFilename.GetPackagesConfigFile();
            }
        }

        public AssemblyBinding Redirect { get; set; }

        public NugetPackage Package { get; set; }
        public string DllVersion { get; set; }

        #endregion Properties
    }
}