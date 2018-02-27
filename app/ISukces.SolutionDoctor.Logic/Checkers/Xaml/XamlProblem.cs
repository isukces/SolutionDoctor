using System;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal abstract class XamlProblem : Problem
    {
        public override ProblemFix GetFix()
        {
            return new ProblemFix(GetFixName(), () =>
            {
                var xml      = XDocument.Load(ProjectFilename.FullName);
                var needSave = false;
                XamlInCsProjChecker.XmlVisitor(xml, q =>
                {
                    var include = q.Include;
                    if (!string.Equals(XamlFile, include, StringComparison.OrdinalIgnoreCase)) return;
                    FixNode(q);
                    q.SubType = "Designer";
                    needSave  = true;
                });
                if (needSave)
                    xml.Save(ProjectFilename.FullName);
            });
        }

        protected abstract void FixNode(CsprojXmlNodeWrapper wrapper);

        protected abstract string GetFixName();

        public string XamlFile { get; set; }
    }
}