using System;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class XamlNotInPageNodeProblem : XamlProblem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine($"File {XamlFile} should be marked as Page");
        }

        public override FixScript GetFixScript()
        {
            return null;
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

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public string DependendUppon { get; set; }
    }
}