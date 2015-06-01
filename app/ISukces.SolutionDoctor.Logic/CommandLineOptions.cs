using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISukces.SolutionDoctor.Logic
{
    public class CommandLineOptions
    {
        #region Constructors

        public CommandLineOptions()
        {
            Directories = new List<string>();
            _options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion Constructors

        #region Static Methods

        // Public Methods 

        public static CommandLineOptions Parse(string[] args)
        {
            if (args == null || args.Length < 1)
                return null;
            var result = new CommandLineOptions();
            string optionName = null;
            foreach (var item in args)
            {
                if (!string.IsNullOrEmpty(optionName))
                {
                    if (IsListOption(optionName))
                    {
                        string current;
                        if (result._options.TryGetValue(optionName, out current))
                            result._options[optionName] = current + "|" + item.Trim();
                        else
                            result._options[optionName] = item;
                    }
                    else
                        result._options[optionName] = item;
                    optionName = null;
                    continue;
                }
                if (item.StartsWith("-"))
                {

                    optionName = item.Substring(1).Trim();
                    if (IsBoolOption(optionName))
                    {
                        result._options[optionName] = "";
                        optionName = null;
                    }
                    continue;
                }
                result.Directories.Add(item);
            }
            {
                string current;
                result._options.TryGetValue("exclude", out current);
                result.Exclude = (current ?? "").Split('|').Select(a => a.ToLower()).Distinct().ToArray();
            }
            return result;
        }

        public string[] Exclude { get; private set; }

        // Private Methods 

        private static bool IsBoolOption(string optionName)
        {
            return
                String.Equals(optionName, "fix", StringComparison.CurrentCultureIgnoreCase)
                || String.Equals(optionName, "onlyBig", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsListOption(string optionName)
        {
            return
                String.Equals(optionName, "exclude", StringComparison.CurrentCultureIgnoreCase);

        }

        #endregion Static Methods

        #region Fields

        readonly Dictionary<string, string> _options;

        #endregion Fields

        #region Properties

        public bool ShowOnlyBigProblems
        {
            get
            {
                return _options.ContainsKey("onlyBig");
            }
        }

        public List<string> Directories { get; private set; }

        public bool Fix
        {
            get
            {
                return _options.ContainsKey("fix");
            }
        }

        #endregion Properties
    }
}
