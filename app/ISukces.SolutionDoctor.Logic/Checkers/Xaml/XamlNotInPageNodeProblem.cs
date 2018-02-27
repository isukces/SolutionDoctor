using System;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class XamlNotInPageNodeProblem : Problem
    {
        public override void Describe(Action<string> writeLine)
        {
            writeLine($"File {XamlFile} should be marked as Page");
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix($"Set {XamlFile} as page", SetXmlNodeNameToPage);
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        private void SetXmlNodeNameToPage()
        {
            var xml      = XDocument.Load(ProjectFilename.FullName);
            var needSave = false;
            XamlInCsProjChecker.XmlVisitor(xml, q =>
            {
                var include = (string)q.Attribute("Include");
                if (!String.Equals(XamlFile, include, StringComparison.OrdinalIgnoreCase)) return;
                q.Name   = q.Name.Namespace + "Page";
                needSave = true;
            });
            if (needSave)
                xml.Save(ProjectFilename.FullName);
        }

        public string XamlFile       { get; set; }
        public string DependendUppon { get; set; }
    }
}