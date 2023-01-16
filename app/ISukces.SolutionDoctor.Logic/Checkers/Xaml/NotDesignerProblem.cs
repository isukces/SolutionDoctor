using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class NotDesignerProblem : XamlProblem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"File {XamlFile} has no Subtype=Designer attribute");
        }

        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            wrapper.SubType = "Designer";
        }

        protected override string GetFixName()
        {
            return $"add Subtype attribute to {XamlFile}";
        }

        public override FixScript GetFixScript()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }
    }
}
