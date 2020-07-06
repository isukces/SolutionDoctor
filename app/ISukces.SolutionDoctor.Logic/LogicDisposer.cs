using ISukces.SolutionDoctor.Logic.Checkers;

namespace ISukces.SolutionDoctor
{
    public class LogicDisposer
    {
        public static void Flush()
        {
            ReferencesWithoutNugetsChecker.Flush();
        }
    }
}