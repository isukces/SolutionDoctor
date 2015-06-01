using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;
using ISukces.SolutionDoctor.Logic.Vs;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic
{
    public class CommandLineOptions
    {
        #region Constructors

        private CommandLineOptions()
        {
            Directories = new List<string>();
            _options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PackagesVersion = new Dictionary<string, NugetVersion>(StringComparer.OrdinalIgnoreCase);
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
                    if (String.Equals(optionName, "cfg", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var tmp = Load(new FileInfo(item));
                        result.Append(tmp);
                    }
                    else if (String.Equals(optionName, "package", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var tmp = item.Split('=');
                        if (tmp.Length != 2)
                            throw new Exception("-package option need parameter: {package id}={version}");
                        result.PackagesVersion[tmp[0].Trim()] = NugetVersion.Parse(tmp[1]);
                    }
                    else
                        result.SetOptionValue(optionName, item);
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
                result.ExcludeSolutions = (current ?? "").Split('|').Select(a => a.ToLower()).Distinct().ToArray();
            }
            return result;
        }
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

        private static CommandLineOptions Load([NotNull] FileInfo file)
        {
            CommandLineOptions o = new CommandLineOptions();
            file.CheckValidForRead();
            var lines = File.ReadAllLines(file.FullName).Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a))
                .ToArray();
            foreach (var i in lines)
            {
                if (i.StartsWith("#") || i.StartsWith(";")) continue;
                if (i.StartsWith("-"))
                {
                    var nameAndValue = i.Substring(1).Trim();
                    var idx = nameAndValue.IndexOfAny(new[] { ' ', '\t' });
                    var name = idx < 0 ? nameAndValue : nameAndValue.Substring(0, idx);
                    if (IsBoolOption(name))
                    {
                        if (idx >= 0)
                            throw new Exception("Option " + name + " needs no parameter");
                        o.SetOptionValue(name, "");
                    }
                    else
                    {
                        if (idx < 0)
                            throw new Exception("Option " + name + " needs parameter");
                        o.SetOptionValue(name, nameAndValue.Substring(idx).Trim());
                    }
                }
                else
                    o.Directories.Add(i);
            }
            return o;
        }

        private static bool SkipOption(string optionName)
        {
            return
                String.Equals(optionName, "saveOptions", StringComparison.CurrentCultureIgnoreCase)
                ||
                String.Equals(optionName, "cfg", StringComparison.CurrentCultureIgnoreCase);
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public void Save([NotNull] FileInfo file)
        {
            if (file == null) throw new ArgumentNullException("file");
            var l = Directories.Select(a => a.Trim()).Distinct().ToList();

            foreach (var i in _options)
            {
                if (SkipOption(i.Key)) continue;
                if (IsListOption(i.Key))
                {
                    var values = i.Value.Split('|').Select(a => a.Trim()).Distinct().ToArray();
                    l.AddRange(values.Select(value => (i.Key + " " + value).Trim()));
                }
                else
                {
                    var value = IsBoolOption(i.Key) ? "" : i.Value;
                    l.Add((i.Key + " " + value).Trim());
                }
            }
            File.WriteAllLines(file.FullName, l.ToArray());
        }
        // Private Methods 

        private void Append([NotNull] CommandLineOptions other)
        {
            if (other == null) throw new ArgumentNullException("other");
            foreach (var i in _options)
                SetOptionValue(i.Key, i.Value);
        }

        private void SetOptionValue(string optionName, string value)
        {
            if (IsListOption(optionName))
            {
                string current;
                if (_options.TryGetValue(optionName, out current))
                    value = current + "|" + value.Trim();
            }
            _options[optionName] = value;
        }

        #endregion Methods

        #region Fields

        readonly Dictionary<string, string> _options;

        #endregion Fields

        #region Properties

        public List<string> Directories { get; private set; }

        public string[] ExcludeSolutions { get; private set; }

        public bool ShowOnlyBigProblems
        {
            get
            {
                return _options.ContainsKey("onlyBig");
            }
        }

        public string SaveConfigFileName
        {
            get
            {
                string fileName;
                if (!_options.TryGetValue("saveOptions", out fileName) || fileName == null)
                    return null;
                fileName = fileName.Trim();
                return string.IsNullOrEmpty(fileName) ? null : fileName;
            }
        }

        public bool Fix
        {
            get
            {
                return _options.ContainsKey("fix");
            }
        }

        public Dictionary<string, NugetVersion> PackagesVersion { get; private set; }

        #endregion Properties
    }
}
