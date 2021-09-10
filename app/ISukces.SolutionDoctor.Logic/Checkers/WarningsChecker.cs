using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    internal class WarningsChecker
    {
        // Public Methods 

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
            CommandLineOptions options)
        {
            var cond = (string)el.Attribute("Condition");
            if (string.IsNullOrEmpty(cond))
                yield break;

            Problem C(string name, Dictionary<string, AddRemoveOption> dict)
            {
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
                        el.Add(node);
                    }

                    node.Value = expected;
                }

                return new Bla(name, expected, project, options);
            }

            yield return C("NoWarn", options.NoWarn);
            yield return C("WarningsAsErrors", options.WarningsAsErrors);
        }


        private IList<Problem> Check()
        {
            var a = _projects.SelectMany(CheckProj).ToList();
            return a;
        }

        private IEnumerable<Problem> CheckProj(SolutionProject project)
        {
            if (project.Kind != VsProjectKind.Old) return new Problem[0];
            var doc   = FileUtils.Load(project.Location);
            var root  = doc.Root;
            var nodes = root.Elements(root.Name.Namespace + "PropertyGroup").ToArray();
            var ps    = new List<Problem>();
            foreach (var n in nodes)
            {
                var p = CheckNode(n, false, project, _options).Where(a => a != null).ToArray();
                ps.AddRange(p);
            }

            return ps;
        }

        private List<SolutionProject> _projects;
        public CommandLineOptions _options;

        private class Bla : Problem
        {
            public Bla(string name, string expected, SolutionProject project, CommandLineOptions opts)
            {
                _name     = name;
                _expected = expected;
                _project  = project;
                _opts     = opts;
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
                        foreach (var n in nodes)
                        {
                            var q = CheckNode(n, true, _project, _opts).Where(a => a != null).ToArray();
                            if (q.Any())
                                save = true;
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

            private readonly string _name;
            private readonly string _expected;
            private readonly SolutionProject _project;
            private readonly CommandLineOptions _opts;
        }
    }
}