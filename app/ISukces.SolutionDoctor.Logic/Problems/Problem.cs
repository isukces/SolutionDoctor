using System;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public abstract class Problem
    {
        public abstract void Describe(Action<string> writeLine);

        public abstract ProblemFix GetFix();

        protected abstract bool GetIsBigProblem();

        public bool IsBigProblem
        {
            get
            {
                return GetIsBigProblem();
            }
        }

        public FileName ProjectFilename { get; set; }
    }
}
