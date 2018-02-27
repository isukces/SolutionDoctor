using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Problems;
using ISukces.SolutionDoctor.Logic.Vs;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
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

        public static void XmlVisitor(XDocument xml, Action<XElement> processElement)
        {
            var root = xml?.Root;
            if (root == null)
                return;            
            var ns = root.Name.Namespace;
            foreach (var itemGroup in root.Elements(ns + "ItemGroup"))
            foreach (var q in itemGroup.Elements())
                processElement(q);
        }

        private static NodeType GetNodeType(XName name)
        {
            switch (name.LocalName)
            {
                case "None":
                    return NodeType.None;
                case "Page":
                    return NodeType.Page;
            }
            return NodeType.Unknown;
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
                        var problem = new XamlNotInPageNodeProblem
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

        private readonly List<Problem> _result = new List<Problem>();
        private          XNamespace    _namespace;

        private enum NodeType
        {
            Unknown,
            None,
            Page
        }
    }
}