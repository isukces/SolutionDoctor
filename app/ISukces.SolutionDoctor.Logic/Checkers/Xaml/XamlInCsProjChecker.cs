using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void XmlVisitor(XDocument xml, Action<CsprojXmlNodeWrapper> processElement)
        {
            var root = xml?.Root;
            if (root == null)
                return;
            var ns = root.Name.Namespace;
            foreach (var itemGroup in root.Elements(ns + "ItemGroup"))
            foreach (var q in itemGroup.Elements())
                processElement(new CsprojXmlNodeWrapper(q));
        }


        private void CheckDependUppon(CsprojXmlNodeWrapper wrapper, List<string> ammyFiles)
        {
            if (wrapper.NodeType != NodeType.Page)
                return;
            if (!wrapper.HasIncludeExtension(".g.xaml")) return;
            var include   = wrapper.Include;
            var shortName = include.Substring(0, include.Length - 7);
            var possi     = shortName + ".ammy";
            var suggestedAmmyParent =
                ammyFiles.FirstOrDefault(name => string.Equals(name, possi, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(suggestedAmmyParent)) return;
            var folder = include.Split('/', '\\');
            folder[folder.Length - 1] = wrapper.DependentUpon;
            var fullDepend = string.Join("\\", folder);
            if (string.Equals(suggestedAmmyParent, fullDepend, StringComparison.OrdinalIgnoreCase))
                return;
            var problem = new NotUnderAmmyProblem
            {
                ProjectFilename     = _currentProjectLocation,
                DependentUpon       = wrapper.DependentUpon,
                ShouldDependentUpon = suggestedAmmyParent,
                XamlFile            = include
            };
            _result.Add(problem);
        }

        private void CheckGenerator(CsprojXmlNodeWrapper wrapper)
        {
            const string generator = "MSBuild:Compile";
            if (wrapper.Generator == generator) return;
            if (wrapper.NodeType == NodeType.Page)
                _result.Add(new InvalidGeneratorProblem
                {
                    ProjectFilename   = _currentProjectLocation,
                    XamlFile          = wrapper.Include,
                    ExpectedGenerator = generator
                });
        }

        private void CheckIfIsInPageNode(CsprojXmlNodeWrapper wrapper)
        {
            var nt = wrapper.NodeType;
            switch (nt)
            {
                case NodeType.Page:
                case NodeType.Unknown:
                case NodeType.Reference:
                    break;
                case NodeType.None:
                {
                    var problem = new XamlNotInPageNodeProblem
                    {
                        ProjectFilename = _currentProjectLocation,
                        XamlFile        = wrapper.Include,
                        DependendUppon  = wrapper.DependentUpon
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
        }

        private void CheckProject(Project project)
        {
            if (project.Kind != CsProjectKind.Old) return;
            _currentProjectLocation = project.Location;
            var xml = XDocument.Load(_currentProjectLocation.FullName);

            // scan for ammy Files
            var ammyFiles = new List<string>();
            XmlVisitor(xml, wrapper =>
            {
                var include = wrapper.Include;
                if (wrapper.HasIncludeExtension(".ammy"))
                    ammyFiles.Add(include);
            });

            XmlVisitor(xml, wrapper =>
            {
                if (!wrapper.HasIncludeExtension(".xaml")) return;
                if (wrapper.NodeType == NodeType.Reference) return;
                CheckSubtypeDesigner(wrapper);
                CheckGenerator(wrapper);
                CheckIfIsInPageNode(wrapper);
                CheckDependUppon(wrapper, ammyFiles);
            });
            XmlVisitor(xml, wrapper =>
            {
                if (!wrapper.HasIncludeExtension(".ammy")) return;
                if (wrapper.NodeType == NodeType.None) return;
                _result.Add(new AmmyProblem
                {
                    ProjectFilename = _currentProjectLocation,
                    Wrapper = wrapper
                });
            });
        }

        private void CheckSubtypeDesigner(CsprojXmlNodeWrapper wrapper)
        {
            if (wrapper.SubType == "Designer") return;
            if (wrapper.NodeType == NodeType.Page)
                _result.Add(new NotDesignerProblem
                {
                    ProjectFilename = _currentProjectLocation,
                    XamlFile        = wrapper.Include
                });
        }

        private readonly List<Problem> _result = new List<Problem>();

        private FileName _currentProjectLocation;
    }

    public class AmmyProblem : Problem
    {
        public override void Describe(Action<string> writeLine)
        {
            writeLine("ammy file " + Wrapper.Include + " should be marked as None");
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix("mark " + Wrapper.Include + " as None", () =>
            {
                
                var xml      = XDocument.Load(ProjectFilename.FullName);
                var needSave = false;
                var file = Wrapper.Include;
                XamlInCsProjChecker.XmlVisitor(xml, q =>
                {
                    var include = q.Include;
                    if (!string.Equals(file, include, StringComparison.OrdinalIgnoreCase)) return;
                    q.WrappedElement.Name = q.WrappedElement.Name.Namespace + "None";
                    // q.SubType = "Designer";
                    needSave  = true;
                });
                if (needSave)
                    xml.Save(ProjectFilename.FullName);
                
                
                // 
            });
            
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public CsprojXmlNodeWrapper Wrapper { get; set; }
    }
}