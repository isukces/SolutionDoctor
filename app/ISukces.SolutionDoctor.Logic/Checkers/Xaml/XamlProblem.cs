using System;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal abstract class XamlProblem : Problem
    {
        protected XamlProblem()
        {
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix(GetFixName(), () =>
            {
                var xml      = FileUtils.Load(ProjectFilename);
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
                    xml.Save2(ProjectFilename);
            });
        }

        protected abstract void FixNode(CsprojXmlNodeWrapper wrapper);

        protected abstract string GetFixName();

        public string XamlFile { get; set; }
    }
}