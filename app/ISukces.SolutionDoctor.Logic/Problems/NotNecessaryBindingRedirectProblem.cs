using System;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class NotNecessaryBindingRedirectProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"not necessary redirection to {Redirect.Name} ver {Redirect.NewVersion}");
        }

        public override ProblemFix GetFix()
        {
            var txt = $"remove redirection to {Redirect.Name} ver {Redirect.NewVersion}";
            return new ProblemFix(txt, FixMethod);
        }

        public override FixScript GetFixScript()
        {
            return null;
        }
        // Protected Methods 

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        private void FixMethod()
        {
            var fn  = ProjectFilename.GetAppConfigFile();
            var xml = new AppConfig(fn);
            if (!xml.Exists)
                throw new Exception(string.Format("Config file {0} doesn't exist", fn.FullName));
            var node = xml.FindByAssemblyIdentity(Redirect.Name);
            if (node == null)
                throw new Exception(string.Format("Redirection for '{0}' not found", Redirect.Name));
            node.XmlElement.Remove();
            xml.Save();
        }

        public FileName ConfigFile
        {
            get { return ProjectFilename.GetPackagesConfigFile(); }
        }

        public AssemblyBinding Redirect { get; set; }
    }
}