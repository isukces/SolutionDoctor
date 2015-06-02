using System;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    public class NugetVersionRange
    {
        public bool _matchAnyVersion;

        #region Static Methods

        // Public Methods 

        public static NugetVersionRange Parse([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            text = text.Trim();
            const string msg = "Invalid version range {0}";
            var minEdge = ParseRangeEdge(text[0], '[', '(');
            var maxEdge = ParseRangeEdge(text[text.Length - 1], ']', ')');

            if (minEdge != RangeEdge.None)
                text = text.Substring(1).Trim();
            if (maxEdge != RangeEdge.None)
                text = text.Substring(0, text.Length - 1).Trim();


            var textArray = text.Split(',');
            if (textArray.Length > 2)
                throw new Exception(string.Format(msg, text));

            var result = new NugetVersionRange
            {
                VersionMin = NugetVersion.Parse(textArray[0]),
                MinEdge = minEdge,
                MaxEdge = maxEdge
            };
            result.VersionMax = textArray.Length == 2
                ? NugetVersion.Parse(textArray[1])
                : result.VersionMin;
            return result;
        }
        // Private Methods 

        static RangeEdge ParseRangeEdge(char c, char t, char f)
        {
            return c == t ? RangeEdge.Inclusive : (c == f ? RangeEdge.Exclusive : RangeEdge.None);
        }

        static string RangeEdgeToString(RangeEdge flag, string inclusiveText, string exclusiveText)
        {
            switch (flag)
            {
                case RangeEdge.Inclusive:
                    return inclusiveText;
                case RangeEdge.Exclusive:
                    return exclusiveText;
            }
            return "";
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public override string ToString()
        {
            if (_matchAnyVersion)
                return "Any version";
            if (VersionMin.ToString() == VersionMax.ToString()
                && MinEdge == RangeEdge.None
                && MaxEdge == RangeEdge.None)
                return VersionMin.ToString();
            return string.Format("{1}{0} x {2}{3}",
                RangeEdgeToString(MinEdge, "<=", "<"),
                VersionMin,
                RangeEdgeToString(MaxEdge, "<=", "<"),
                VersionMax
                );
        }

        #endregion Methods

        #region Properties

        public NugetVersion VersionMin { get; set; }

        public NugetVersion VersionMax { get; set; }

        public RangeEdge MinEdge { get; set; }

        public RangeEdge MaxEdge { get; set; }

        public static NugetVersionRange Any
        {
            get
            {
                return new NugetVersionRange()
                {
                    _matchAnyVersion = true
                };
            }
        }

        #endregion Properties
    }
}