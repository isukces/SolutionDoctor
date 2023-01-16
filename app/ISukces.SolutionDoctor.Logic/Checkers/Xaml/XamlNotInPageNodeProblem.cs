using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class XamlNotInPageNodeProblem : XamlProblem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"File {XamlFile} should be marked as Page");
        }


        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            wrapper.WrappedElement.Name = wrapper.WrappedElement.Name.Namespace + "Page";
            wrapper.Generator           = XamlInCsProjChecker.XamlGenerator;
            wrapper.SubType             = "Designer";
        }

        protected override string GetFixName()
        {
            return $"Set {XamlFile} as page";
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

        public string DependendUppon { get; set; }

        #endregion
    }
}
