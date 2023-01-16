using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal abstract class XamlProblem : Problem
    {
        protected abstract void FixNode(CsprojXmlNodeWrapper wrapper);

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

        protected abstract string GetFixName();

        #region properties

        public string XamlFile { get; set; }

        #endregion
    }
}
