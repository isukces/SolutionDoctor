using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISukces.SolutionDoctor.Logic.Problems
{
    public class ProblemFix
    {
        public string Description { get; private set; }
        private readonly Action _fixAction;

        public ProblemFix(string description, Action fixAction)
        {
            Description = description;
            _fixAction = fixAction;
        }
        public void Fix()
        {
            _fixAction();
        }
    }
}
