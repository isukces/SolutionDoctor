using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using isukces.json;
using ISukces.SolutionDoctor.Logic.NuGet;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic
{
    public class CommandLineOptions
    {
        private CommandLineOptions()
        {
            ScanDirectories    = new List<string>();
            RemoveBindingRedirect = new HashSet<string>();
            ExcludeDll = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExcludeSolutions   = new List<string>();
            ExcludeDirectories = new List<string>();
            _options           = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PackagesVersion    = new Dictionary<string, NugetVersion>(StringComparer.OrdinalIgnoreCase);
        }

        // Public Methods 

        public static CommandLineOptions Parse(string[] args)
        {
            if (args == null || args.Length < 1)
                return null;
            var    result     = new CommandLineOptions();
            string optionName = null;
            foreach (var item in args)
            {
                optionName = optionName?.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(optionName))
                {
                    switch (optionName)
                    {
                        case "cfg":
                            var fi          = new FileInfo(item);
                            var lineOptions = Load(fi);
                            if (lineOptions == null)
                                throw new Exception("Unable to load " + fi.FullName);
                            result.Append(lineOptions);
                            break;
                        case "excludesolution":
                            result.ExcludeSolutions.Add(item);
                            break;
                        case "excludedir":
                        case "excludedirectory":
                            result.ExcludeDirectories.Add(item);
                            break;
                        case "package":
                            var tmp = item.Split('=');
                            if (tmp.Length != 2)
                                throw new Exception("-package option need parameter: {package id}={version}");
                            result.PackagesVersion[tmp[0].Trim()] = NugetVersion.Parse(tmp[1]);
                            break;
                        case "excludedll":                            
                            result.ExcludeDll.Add(item);
                            break;
                        case "removerebindingredirect":                            
                            result.RemoveBindingRedirect.Add(item);
                            break;
                        default:
                            result.SetOptionValue(optionName, item);
                            break;
                    }

                    optionName = null;
                    continue;
                }

                if (item.StartsWith("-"))
                {
                    optionName = item.Substring(1).Trim();
                    if (IsBoolOption(optionName))
                    {
                        result._options[optionName] = "";
                        optionName                  = null;
                    }

                    continue;
                }

                result.ScanDirectories.Add(item);
            }

            return result;
        }

        private static bool IsBoolOption(string optionName)
        {
            return
                string.Equals(optionName, "fix", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(optionName, "onlyBig", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsListOption(string optionName)
        {
            return
                string.Equals(optionName, "exclude", StringComparison.CurrentCultureIgnoreCase);
        }

        private static CommandLineOptions Load([NotNull] FileInfo file)
        {
            return JsonUtils.Default.Load<CommandLineOptions>(file);
        }

        private static List<string> NormalizeList(List<string> x)
        {
            var q = from i in x
                let j = i?.Trim()
                where !string.IsNullOrEmpty(j)
                select j;
            x = q.Distinct().ToList();
            return x;
        }

        private static bool SkipOption(string optionName)
        {
            return
                string.Equals(optionName, "saveOptions", StringComparison.CurrentCultureIgnoreCase)
                ||
                string.Equals(optionName, "cfg", StringComparison.CurrentCultureIgnoreCase);
        }

        public void Normalize()
        {
            ScanDirectories    = NormalizeList(ScanDirectories);
            ExcludeDirectories = NormalizeList(ExcludeDirectories);
            ExcludeSolutions   = NormalizeList(ExcludeSolutions);
        }

        // Public Methods 

        public void Save([NotNull] FileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            JsonUtils.Default.Save(file, this);
        }

        // Private Methods 

        private void Append([NotNull] CommandLineOptions other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (other.ScanDirectories != null)
                ScanDirectories.AddRange(other.ScanDirectories);
            if (other.ExcludeSolutions != null)
                ExcludeSolutions.AddRange(other.ExcludeSolutions);
            if (other.ExcludeDirectories != null)
                ExcludeDirectories.AddRange(other.ExcludeDirectories);
            if (other.ExcludeDll != null)
                foreach (var i in other.ExcludeDll)
                    ExcludeDll.Add(i);
            
            if (other.RemoveBindingRedirect != null)
                foreach (var i in other.RemoveBindingRedirect)
                    RemoveBindingRedirect.Add(i);

            foreach (var i in other._options)
                SetOptionValue(i.Key, i.Value);
            foreach (var i in other.PackagesVersion)
                PackagesVersion[i.Key] = i.Value;

            // Fix = other.ShowOnlyBigProblems;
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

        public HashSet<string> ExcludeDll { get; }
        
        public HashSet<string> RemoveBindingRedirect { get; }

        public List<string> ScanDirectories { get; private set; }

        public List<string> ExcludeDirectories { get; private set; }

        public List<string> ExcludeSolutions { get; private set; }


        public bool ShowOnlyBigProblems
        {
            get { return _options.ContainsKey("onlyBig"); }
            set
            {
                if (value)
                    _options["onlyBig"] = "";
                else
                    _options.Remove("onlyBig");
            }
        }

        [JsonIgnore]
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
            get { return _options.ContainsKey("fix"); }
            set
            {
                if (value)
                    _options["fix"] = "";
                else
                    _options.Remove("fix");
            }
        }

        public Dictionary<string, NugetVersion> PackagesVersion { get; private set; }

        private readonly Dictionary<string, string> _options;
    }
}