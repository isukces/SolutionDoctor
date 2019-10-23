using System;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class ProblemFix
    {
        public ProblemFix(string description, Action fixAction)
        {
            Description = description;
            _fixAction  = fixAction;
        }

        public void Fix()
        {
            _fixAction();
        }

        public string Description { get; }
        private readonly Action _fixAction;
    }
}