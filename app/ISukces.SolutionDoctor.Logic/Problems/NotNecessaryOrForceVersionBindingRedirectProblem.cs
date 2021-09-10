using System;
using iSukces.Code.VsSolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class NotNecessaryOrForceVersionBindingRedirectProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            if (Version != null)
                writeLine($"force redirection {Redirect.Name} ver {Redirect.NewVersion} -> {Version}");
            else
                writeLine($"not necessary redirection to {Redirect.Name} ver {Redirect.NewVersion}");
        }

        public override ProblemFix GetFix()
        {
            string txt;
            if (Version != null)
                txt = $"change redirection to {Redirect.Name} ver {Version}";
            else
                txt = $"remove redirection to {Redirect.Name} ver {Redirect.NewVersion}";
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
            AssemblyBinding node = xml.FindByAssemblyIdentity(Redirect.Name);
            if (node == null)
                throw new Exception(string.Format("Redirection for '{0}' not found", Redirect.Name));
            if (Version != null)
                node.SetRedirection(this.Version.ToString());
            else
                node.XmlElement.Remove();
            xml.Save();
        }

        public FileName ConfigFile
        {
            get { return ProjectFilename.GetPackagesConfigFile(); }
        }

        public AssemblyBinding Redirect { get; set; }
        public NugetVersion    Version  { get; set; }
    }
}