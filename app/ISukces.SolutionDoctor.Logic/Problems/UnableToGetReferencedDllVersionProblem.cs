using System;
using isukces.code.vssolutions;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class UnableToGetReferencedDllVersionProblem : Problem
    {
        public UnableToGetReferencedDllVersionProblem(string what, SolutionProject project, string why)
        {
            _what           = what;
            _why            = why;
            ProjectFilename = project.Location;
        }

        public override void Describe(Action<RichString> writeLine)
        {
            writeLine("Unable to get DLL version for " + _what);
            writeLine("    " + _why);
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        public override FixScript GetFixScript()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        private readonly string _what;
        private readonly string _why;
    }
}