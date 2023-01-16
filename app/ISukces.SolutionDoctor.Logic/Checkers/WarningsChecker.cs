using System.Text.RegularExpressions;
using System.Xml.Linq;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class WarningsChecker
    {
        public static IEnumerable<Problem> Check(List<SolutionProject> projects, CommandLineOptions options)
        {
            // var aa = localNugetRepositiories.GetUnique(a => a.Location.FullName.ToLower(), a => a);

            var checker = new WarningsChecker
            {
                _projects = projects.ToList(),
                _options  = options
            };
            return checker.Check();
        }

        private static IEnumerable<Problem> CheckNode(XElement el, bool fix, SolutionProject project,
            CommandLineOptions options, string configuration, XElement root)
        {
            yield return CreateProblem("NoWarn", options.NoWarn, el, fix, project, options, configuration, root);
            yield return CreateProblem("WarningsAsErrors", options.WarningsAsErrors, el, fix, project, options, configuration, root);
        }

        private static Problem CreateProblem(string name, Dictionary<string, AddRemoveOption> dict, XElement el, bool fix,
            SolutionProject project, CommandLineOptions options, string configuration, XElement root)
        {
            if (el is null)
            {
                if (string.IsNullOrEmpty(configuration))
                    throw new NotSupportedException();
                var ns  = root.Name.Namespace + "PropertyGroup";
                var all = root.Elements(ns).ToArray();
                el = all.FirstOrDefault(a => string.Equals(Info1.FromNode(a)?.Configuration, configuration, StringComparison.OrdinalIgnoreCase));
                if (el is null)
                {
                    var first = all.FirstOrDefault();
                    el = new XElement(ns, new XAttribute("Condition", GetCondition(configuration)));
                    if (first is null)
                    {
                        root.Add(el);
                    }
                    else
                    {
                        first.AddAfterSelf(el);
                    }
                }
            }

            var node = el.Element(el.Name.Namespace + name);
            var nn   = node?.Value ?? string.Empty;
            var q    = new HashSet<string>();
            foreach (var ii in nn.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)))
                q.Add(ii);

            var q1 = dict.OrderBy(a => a.Key).ToArray();
            foreach (var i in q1)
                if (i.Value == AddRemoveOption.Add)
                    q.Add(i.Key);
                else
                    q.Remove(i.Key);

            var expected = string.Join(",", q.OrderBy(a => a).ToArray());
            if (expected == nn)
                return null;
            if (fix)
            {
                if (node == null)
                {
                    node = new XElement(el.Name.Namespace + name);
                    if (!string.IsNullOrEmpty(configuration))
                        node.SetAttributeValue("Condition", GetCondition(configuration));
                    el.Add(node);
                }

                node.Value = expected;
            }

            return new Bla(name, expected, project, options, configuration);
        }

        private static string GetCondition(string configuration)
        {
            return $"'$(Configuration)' == '{configuration}'";
        }

        private IList<Problem> Check()
        {
            var a = _projects.SelectMany(CheckProj).ToList();
            return a;
        }

        private IEnumerable<Problem> CheckProj(SolutionProject project)
        {
            if (project.Kind != VsProjectKind.Legacy && project.Kind != VsProjectKind.Core) return Array.Empty<Problem>();
            var doc   = FileUtils.Load(project.Location);
            var root  = doc.Root;
            var nodes = root.Elements(root.Name.Namespace + "PropertyGroup").ToArray();
            var ps    = new List<Problem>();
            var infos = new List<Info1>();
            foreach (var n in nodes)
            {
                var info = Info1.FromNode(n);
                if (info is null)
                    continue;
                infos.Add(info);
                var p = CheckNode(n, false, project, _options, null, root).Where(a => a != null).ToArray();
                ps.AddRange(p);
            }

            foreach (var i in "Debug,Release".Split(','))
            {
                if (infos.Any(a => string.Equals(a.Configuration, i, StringComparison.OrdinalIgnoreCase))) continue;
                {
                    var p = CheckNode(null, false, project, _options, i, root).Where(a => a != null).ToArray();
                    ps.AddRange(p);
                }
            }

            return ps;
        }

        #region Fields

        const string ConfigurationDebugReleaseFilter = @"'\$\(Configuration\)'=='(Debug|Release)'";
        static readonly Regex ConfigurationDebugReleaseRegex = new Regex(ConfigurationDebugReleaseFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private List<SolutionProject> _projects;
        public CommandLineOptions _options;

        #endregion

        class Info1
        {
            public Info1(XElement el, string configuration)
            {
                El            = el;
                Configuration = configuration;
            }

            public static Info1 FromNode(XElement el)
            {
                var cond = (string)el.Attribute("Condition") ?? "";
                cond = cond.Replace(" ", "");
                var m = ConfigurationDebugReleaseRegex.Match(cond);
                if (!m.Success)
                    return null;
                return new Info1(el, m.Groups[1].Value);
            }

            #region properties

            public XElement El            { get; }
            public string   Configuration { get; }

            #endregion
        }


        private class Bla : Problem
        {
            public Bla(string name, string expected, SolutionProject project, CommandLineOptions opts,
                string configuration)
            {
                // configuration jeśli trzeba dodać
                _name          = name;
                _expected      = expected;
                _project       = project;
                _opts          = opts;
                _configuration = configuration;
            }

            public override void Describe(Action<RichString> writeLine)
            {
                writeLine("Correct " + _name + " to " + _expected);
            }

            public override ProblemFix GetFix()
            {
                return new ProblemFix("Correct " + _name, () =>
                {
                    lock(Locking.Lock)
                    {
                        var doc   = FileUtils.Load(_project.Location);
                        var root  = doc.Root;
                        var nodes = root.Elements(root.Name.Namespace + "PropertyGroup").ToArray();
                        var save  = false;
                        var infos = new List<Info1>();
                        foreach (var n in nodes)
                        {
                            var info = Info1.FromNode(n);
                            if (info is null)
                                continue;
                            infos.Add(info);

                            var q = CheckNode(n, true, _project, _opts, null, root).Where(a => a != null).ToArray();
                            if (q.Any())
                                save = true;
                        }

                        foreach (var i in "Debug,Release".Split(','))
                        {
                            if (infos.Any(a => string.Equals(a.Configuration, i, StringComparison.OrdinalIgnoreCase))) continue;
                            {
                                var p = CheckNode(null, false, _project, _opts, i, root).Where(a => a != null).ToArray();
                                if (p.Any())
                                    save = true;
                            }
                        }

                        if (save)
                            doc.Save2(_project.Location);
                    }
                });
            }

            public override FixScript GetFixScript()
            {
                return null;
            }

            protected override bool GetIsBigProblem()
            {
                return true;
            }

            #region Fields

            private readonly string _name;
            private readonly string _expected;
            private readonly SolutionProject _project;
            private readonly CommandLineOptions _opts;
            private readonly string _configuration;

            #endregion
        }
    }
}
