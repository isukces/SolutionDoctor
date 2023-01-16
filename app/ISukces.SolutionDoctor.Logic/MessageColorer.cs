using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic
{
    public class MessageColorer
    {
        public void Color(RichString.FormatContext ctx)
        {
            if (!ctx.Before) return;
            ctx.AutoResetColorsAfter = true;

            ConsoleColor? G()
            {
                if (_projects.Contains(ctx.ParameterIndex))
                    return ConsoleColor.Cyan;
                if (_packages.Contains(ctx.ParameterIndex))
                    return ConsoleColor.Blue;
                if (_versions.Contains(ctx.ParameterIndex))
                    return ConsoleColor.Yellow;
                return null;
            }

            var color = G();
            if (color.HasValue)
                ctx.SetTextColor(color.Value);
        }

        private MessageColorer With(ISet<int> projects, int[] indexes)
        {
            foreach (var i in indexes)
                projects.Add(i);
            return this;
        }

        public MessageColorer WithPackageAt(params int[] indexes)
        {
            return With(_packages, indexes);
        }

        public MessageColorer WithProjectAt(params int[] indexes)
        {
            return With(_projects, indexes);
        }

        public MessageColorer WithVersionAt(params int[] indexes)
        {
            return With(_versions, indexes);
        }

        #region Fields

        private readonly HashSet<int> _projects = new HashSet<int>();
        private readonly ISet<int> _packages = new HashSet<int>();
        private readonly ISet<int> _versions = new HashSet<int>();

        #endregion
    }
}
