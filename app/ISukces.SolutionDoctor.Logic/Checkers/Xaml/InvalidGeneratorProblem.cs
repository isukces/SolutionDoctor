using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class InvalidGeneratorProblem : XamlProblem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"file {XamlFile} should have {ExpectedGenerator} generator attribute");
        }

        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            wrapper.Generator = ExpectedGenerator;
        }

        protected override string GetFixName()
        {
            return $"set Generator={ExpectedGenerator} to {XamlFile}";
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

        public string ExpectedGenerator { get; set; }

        #endregion
    }
}
