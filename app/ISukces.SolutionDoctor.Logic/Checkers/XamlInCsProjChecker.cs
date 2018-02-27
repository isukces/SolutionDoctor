using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    public class XamlInCsProjChecker
    {
        private XamlInCsProjChecker()
        {
        }

        public static IList<Problem> Check(List<Project> projs)
        {
            var a = new XamlInCsProjChecker();
            foreach (var project in projs) a.CheckProject(project);

            return a._result;
        }

        public static void XmlVisitor(XDocument xml, Action<XElement> el)
        {
            var ns = xml.Root.Name.Namespace;

            foreach (var itemGroup in xml.Root.Elements(ns + "ItemGroup"))
            foreach (var q in itemGroup.Elements())
                el(q);
        }

        private void CheckProject(Project project)
        {
            if (project.Kind != CsProjectKind.Old) return;

            var xml = XDocument.Load(project.Location.FullName);
            _namespace = xml.Root.Name.Namespace;
            XmlVisitor(xml, q =>
            {
                var include = (string)q.Attribute("Include");
                if (string.IsNullOrEmpty(include)) return;
                if (!include.ToLower().EndsWith(".xaml")) return;
                var nt = GetNodeType(q.Name);

                switch (nt)
                {
                    case NodeType.Page:
                    case NodeType.Unknown:
                        return;
                    case NodeType.None:
                    {
                        var problem = new NotAPageProblem
                        {
                            ProjectFilename = project.Location,
                            XamlFile        = include,
                            DependendUppon  = q.Element(_namespace + "DependentUpon")?.Value
                        };
                        _result.Add(problem);
                        /*
 przykład błędnego                         
<None Include="DrawingPanel.g.xaml">
      <SubType>Designer</SubType>
      <Generator>MSBuild:Compile</Generator>
      <DependentUpon>DrawingPanel.ammy</DependentUpon>
</None>
                         */
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private NodeType GetNodeType(XName name)
        {
            if (name == _namespace + "None")
                return NodeType.None;
            if (name == _namespace + "Page")
                return NodeType.Page;
            return NodeType.Unknown;
        }

        private readonly List<Problem> _result = new List<Problem>();
        private          XNamespace    _namespace;

        private enum NodeType
        {
            Unknown,
            None,
            Page
        }

        private class NotAPageProblem : Problem
        {
            public override void Describe(Action<string> writeLine)
            {
                writeLine($"File {XamlFile} should be marked as Page");
            }

            public override ProblemFix GetFix()
            {
                return new ProblemFix($"Set {XamlFile} as page", SetXmlNodeNameToPage);
            }

            protected override bool GetIsBigProblem()
            {
                return true;
            }

            private void SetXmlNodeNameToPage()
            {
                var xml = XDocument.Load(ProjectFilename.FullName);
                var needSave = false;
                XmlVisitor(xml, q =>
                {
                    var include = (string)q.Attribute("Include");
                    if (string.Equals(XamlFile, include, StringComparison.OrdinalIgnoreCase))
                    {
                        q.Name = q.Name.Namespace + "Page";
                        needSave = true;
                    }
                });
                if (needSave)
                    xml.Save(ProjectFilename.FullName);
            }

            public string XamlFile       { get; set; }
            public string DependendUppon { get; set; }
        }
    }
}