#region using

using System;
using ISukces.SolutionDoctor.Logic.Vs;

#endregion

namespace ISukces.SolutionDoctor.Logic.Problems
{
    internal class UnableToGetReferencedDllVersionProblem : Problem
    {
        #region Constructors

        public UnableToGetReferencedDllVersionProblem(string what, Project project, string why)
        {
            _what = what;
            _why = why;
            ProjectFilename = project.Location;
        }

        #endregion

        #region Instance Methods

        public override void Describe(Action<string> writeLine)
        {
            writeLine("Unable to get DLL version for " + _what);
            writeLine("    " + _why);
        }

        public override ProblemFix GetFix()
        {
            return null;
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        #endregion

        #region Fields

        private readonly string _what;
        private readonly string _why;

        #endregion
    }
}