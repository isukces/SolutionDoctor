﻿using iSukces.Code.VsSolutions;
using isukces.json;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic
{
    public class CommandLineOptions : IDoctorConfig
    {
        private CommandLineOptions()
        {
            ScanDirectories       = new List<string>();
            RemoveBindingRedirect = new HashSet<string>();
            ForceBindingRedirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ExcludeDll            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExcludeSolutions      = new List<string>();
            ExcludeDirectories    = new List<string>();
            _options              = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PackagesVersion       = new Dictionary<string, NugetVersion>(StringComparer.OrdinalIgnoreCase);
            WarningsAsErrors      = new Dictionary<string, AddRemoveOption>(StringComparer.OrdinalIgnoreCase);
            NoWarn                = new Dictionary<string, AddRemoveOption>(StringComparer.OrdinalIgnoreCase);
        }


        private static void AddRemove(IDictionary<string, AddRemoveOption> hs, string item)
        {
            if (string.IsNullOrEmpty(item))
                return;
            var itemArray = item.Split(',');
            foreach (var src in itemArray)
            {
                var key = src.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;
                var minus = key[0] == '-';
                if (minus)
                {
                    key = key.Substring(1).Trim();
                    if (string.IsNullOrEmpty(key))
                        continue;
                }

                if (minus)
                    hs[key] = AddRemoveOption.Remove;
                else
                    hs[key] = AddRemoveOption.Add;
            }
        }

        private static string ExpandPath(FileInfo baseFile, string file)
        {
            if (baseFile is null)
                return file;
            return Path.Combine(baseFile.Directory.FullName, file);
        }

        private static bool IsBoolOption(string optionName)
        {
            return
                string.Equals(optionName, "fix", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(optionName, "onlyBig", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(optionName, "runExternalFix", StringComparison.CurrentCultureIgnoreCase);
            ;
        }

        private static bool IsListOption(string optionName)
        {
            return
                string.Equals(optionName, "exclude", StringComparison.CurrentCultureIgnoreCase);
        }

        private static CommandLineOptions Load([NotNull] FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException(file.FullName); 
            var x = JsonUtils.Default.Load<CommandLineOptions>(file);
            
            x.ApplyPath(file);
            return x;
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
                    string[] tmp;
                    switch (optionName)
                    {
                        case "runExternalFix":
                            result.RunExternalFix = true;
                            break;
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
                            tmp = item.Split('=');
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

                        case "nowarn":
                            AddRemove(result.NoWarn, item);
                            break;
                        case "forceredirect":
                            tmp = item.Split('=');
                            if (tmp.Length != 2)
                                throw new Exception("-forceRedirects option need parameter: {package id}={version}");
                            result.ForceBindingRedirects[tmp[0].Trim()] = tmp[1];
                            break;
                        case "warningsaserrors":
                            AddRemove(result.WarningsAsErrors, item);
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

        private static string ReduceFilename(FileInfo baseFile, string target)
        {
            try
            {
                var uri = new Uri(baseFile.FullName);

                var result = uri.MakeRelativeUri(new Uri(target)).ToString();
                var ii     = result.Replace("/", "\\");
                return ii;
            }
            catch
            {
                return target;
            }
        }

        private static bool SkipOption(string optionName)
        {
            return
                string.Equals(optionName, "saveOptions", StringComparison.CurrentCultureIgnoreCase)
                ||
                string.Equals(optionName, "cfg", StringComparison.CurrentCultureIgnoreCase);
        }


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
            if (other.SkippedRules != null)
            {
                if (SkippedRules is null)
                    SkippedRules = new List<SkipRule>();
                foreach (var i in other.SkippedRules)
                    SkippedRules.Add(i);
            }

            if (other.RemoveBindingRedirect != null)
                foreach (var i in other.RemoveBindingRedirect)
                    RemoveBindingRedirect.Add(i);

            foreach (var i in other._options)
                SetOptionValue(i.Key, i.Value);
            foreach (var i in other.PackagesVersion)
                PackagesVersion[i.Key] = i.Value;

            foreach (var i in other.NoWarn)
                NoWarn[i.Key] = i.Value;
            foreach (var i in other.WarningsAsErrors)
                WarningsAsErrors[i.Key] = i.Value;
            foreach (var i in other.ForceBindingRedirects)
                ForceBindingRedirects[i.Key] = i.Value;

            if (other.SolutionOrders != null)
            {
                var x = SolutionOrders ?? new List<string>();
                x.AddRange(other.SolutionOrders);
                SolutionOrders = x.Distinct().ToList();
            }

            if (!string.IsNullOrEmpty(other.LangVersion))
                LangVersion = other.LangVersion;
            // Fix = other.ShowOnlyBigProblems;
        }

        private void ApplyPath(FileInfo file)
        {
            if (file == null) return;
            if (SolutionOrders != null)
                for (var index = 0; index < SolutionOrders.Count; index++)
                    SolutionOrders[index] = ExpandPath(file, SolutionOrders[index]);
        }

        public bool IsSkipped(string rule, FileName projectLocation)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (SkippedRules is null)
                return false;

            return SkippedRules.Any(a =>
            {
                var fn = Path.Combine(currentDirectory, a.Project);
                var fi = new FileInfo(fn);
                return a.Rule == rule &&
                       string.Equals(fi.FullName, projectLocation.FullName, StringComparison.OrdinalIgnoreCase);
            });
        }

        public void Normalize()
        {
            ScanDirectories    = NormalizeList(ScanDirectories);
            ExcludeDirectories = NormalizeList(ExcludeDirectories);
            ExcludeSolutions   = NormalizeList(ExcludeSolutions);
            SkippedRules = SkippedRules?.Distinct()
                .OrderBy(a => a.Project)
                .ThenBy(a => a.Rule)
                .ToList() ?? new List<SkipRule>();
        }

        private void ReducePath(FileInfo file)
        {
            if (file == null) return;
            if (SolutionOrders != null)
                for (var index = 0; index < SolutionOrders.Count; index++)
                {
                    SolutionOrders[index] = ReduceFilename(file, SolutionOrders[index]);
                    ;
                }
        }

        public void Save([NotNull] FileInfo file)
        {
            Normalize();
            if (file == null) throw new ArgumentNullException(nameof(file));
            ReducePath(file);
            JsonUtils.Default.Save(file, this);
            ApplyPath(file);
        }

        private void SetOptionValue(string optionName, string value)
        {
            if (IsListOption(optionName))
            {
                if (_options.TryGetValue(optionName, out var current))
                    value = current + "|" + value.Trim();
            }

            _options[optionName] = value;
        }

        #region properties

        public string LangVersion
        {
            get => _langVersion;
            set => _langVersion = value?.Trim();
        }

        public Dictionary<string, string> ForceBindingRedirects { get; }

        public HashSet<string> ExcludeDll { get; }

        public HashSet<string> RemoveBindingRedirect { get; }

        public List<string> ScanDirectories { get; private set; }

        public List<string> ExcludeSolutions { get; private set; }

        public List<string> SolutionOrders { get; set; }

        public List<SkipRule> SkippedRules { get; set; }


        public bool ShowOnlyBigProblems
        {
            get => _options.ContainsKey("onlyBig");
            set
            {
                if (value)
                    _options["onlyBig"] = "";
                else
                    _options.Remove("onlyBig");
            }
        }

        public bool RunExternalFix
        {
            get => _options.ContainsKey("runExternalFix");
            set
            {
                if (value)
                    _options["runExternalFix"] = "";
                else
                    _options.Remove("runExternalFix");
            }
        }

        [JsonIgnore]
        public string SaveConfigFileName
        {
            get
            {
                if (!_options.TryGetValue("saveOptions", out var fileName) || fileName == null)
                    return null;
                fileName = fileName.Trim();
                return string.IsNullOrEmpty(fileName) ? null : fileName;
            }
        }

        public bool Fix
        {
            get => _options.ContainsKey("fix");
            set
            {
                if (value)
                    _options["fix"] = "";
                else
                    _options.Remove("fix");
            }
        }

        public Dictionary<string, NugetVersion> PackagesVersion { get; }

        #endregion

        public Dictionary<string, AddRemoveOption> NoWarn { get; }

        public Dictionary<string, AddRemoveOption> WarningsAsErrors { get; }

        public List<string> ExcludeDirectories { get; private set; }

        #region Fields

        private readonly Dictionary<string, string> _options;
        private string _langVersion;

        #endregion
    }
}
