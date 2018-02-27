using System;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class InvalidGeneratorProblem : XamlProblem
    {
        public string ExpectedGenerator { get; set; }
        public override void Describe(Action<string> writeLine)
        {
            writeLine($"file {XamlFile} should have {ExpectedGenerator} generator attribute");
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            wrapper.Generator = ExpectedGenerator;
        }

        protected override string GetFixName()
        {
            return $"set Generator={ExpectedGenerator} to {XamlFile}";
        }
    }
}