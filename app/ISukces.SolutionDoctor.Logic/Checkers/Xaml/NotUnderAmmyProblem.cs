using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class NotUnderAmmyProblem : XamlProblem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"File {XamlFile} should depend uppon {ShouldDependentUpon}");
        }

        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            var ammyFi = new FileInfo(ShouldDependentUpon);
            wrapper.DependentUpon = ammyFi.Name;
        }

        public override ProblemFix GetFix()
        {
            var xamlFi = new FileInfo(XamlFile);
            var ammyFi = new FileInfo(ShouldDependentUpon);
            if (xamlFi.Directory?.FullName == ammyFi.Directory?.FullName)
                return base.GetFix();
            return null;
        }

        protected override string GetFixName()
        {
            var ammyFi = new FileInfo(ShouldDependentUpon);
            return "move uppon " + ammyFi.Name;
        }

        public override FixScript GetFixScript()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #region properties

        public string DependentUpon       { get; set; }
        public string ShouldDependentUpon { get; set; }

        #endregion
    }
}
