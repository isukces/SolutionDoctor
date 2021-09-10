using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    public class XamlInCsProjChecker
    {
        private readonly CommandLineOptions _options;

        private XamlInCsProjChecker(CommandLineOptions options)
        {
            _options = options;
        }

        public static IEnumerable<Problem> Check(List<SolutionProject> projs, CommandLineOptions options)
        {
            var a = new XamlInCsProjChecker(options);
            foreach (var project in projs)
                a.CheckProject(project);

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
            if (wrapper.Generator == XamlGenerator) return;
            if (wrapper.NodeType == NodeType.Page)
                _result.Add(new InvalidGeneratorProblem
                {
                    ProjectFilename   = _currentProjectLocation,
                    XamlFile          = wrapper.Include,
                    ExpectedGenerator = XamlGenerator
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
                case NodeType.Content:
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

        private void CheckProject(SolutionProject project)
        {
            if (project.Kind != VsProjectKind.Old) return;
            if (_options.IsSkipped(nameof(XamlInCsProjChecker), project.Location))
                    return;
            _currentProjectLocation = project.Location;
            var xml = FileUtils.Load(_currentProjectLocation);

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
                    Wrapper         = wrapper
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


        public const string XamlGenerator = "MSBuild:Compile";
    }

    public class AmmyProblem : Problem
    {
        public override void Describe(Action<RichString> writeLine)
        {
            writeLine("ammy file " + Wrapper.Include + " should be marked as None");
        }

        public override ProblemFix GetFix()
        {
            return new ProblemFix("mark " + Wrapper.Include + " as None", () =>
            {
                var xml      = FileUtils.Load(ProjectFilename);
                var needSave = false;
                var file     = Wrapper.Include;
                XamlInCsProjChecker.XmlVisitor(xml, q =>
                {
                    var include = q.Include;
                    if (!string.Equals(file, include, StringComparison.OrdinalIgnoreCase)) return;
                    q.WrappedElement.Name = q.WrappedElement.Name.Namespace + "None";
                    // q.SubType = "Designer";
                    needSave = true;
                });
                if (needSave)
                    xml.Save2(ProjectFilename);

                // 
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

        public CsprojXmlNodeWrapper Wrapper { get; set; }
    }
}