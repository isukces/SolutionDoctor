using System;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public abstract class Problem
    {
        #region Methods

        // Public Methods 

        public abstract void Describe(Action<string> writeLine);

        public abstract ProblemFix GetFix();
        // Protected Methods 

        protected abstract bool GetIsBigProblem();

        #endregion Methods

        #region Properties

        public bool IsBigProblem
        {
            get
            {
                return GetIsBigProblem();
            }
        }

        public string ProjectFilename { get; set; }

        #endregion Properties
    }
}
