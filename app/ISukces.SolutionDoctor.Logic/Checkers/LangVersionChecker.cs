using System.Xml.Linq;
using iSukces.Code.VsSolutions;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers
{
    public class LangVersionChecker
    {
        public static Problem Check(SolutionProject project, CommandLineOptions options, bool fix)
        {
            if (project.Kind != VsProjectKind.Core)
                return null;

            if (fix)
            {
                lock(Locking.Lock)
                {
                    return DoInternal();
                }
            }

            return DoInternal();

            LangVersionProblem DoInternal()
            {
                bool     save  = false;
                var      doc   = FileUtils.Load(project.Location);
                XElement root  = doc.Root;
                var      nodes = root.Elements(root.Name.Namespace + "PropertyGroup").ToArray();

                var found    = false;
                var anyToFix = new List<string>();
                foreach (var i in nodes)
                {
                    var lang = i.Elements(root.Name.Namespace + "LangVersion").ToArray();
                    if (lang.Length == 0)
                        continue;
                    found = true;
                    foreach (var j in lang)
                    {
                        if (j.Value == options.LangVersion)
                            continue;
                        if (fix)
                        {
                            anyToFix.Add(options.LangVersion);
                        }
                        else
                        {
                            j.Value = options.LangVersion;
                            save    = true;
                        }
                    }
                }

                if (fix && !found)
                {
                    var nodes2 = root.Elements(root.Name.Namespace + "PropertyGroup").ToArray();
                    nodes2.First().Add(new XElement(root.Name.Namespace + "LangVersion", options.LangVersion));
                    save = true;
                }

                if (save && fix)
                    doc.Save2(project.Location);

                if (anyToFix.Count > 0 || !found)
                {
                    return new LangVersionProblem(found, anyToFix, project, () =>
                    {
                        Check(project, options, true);
                    }, options.LangVersion);
                }

                return null;
            }
        }

        public static IEnumerable<Problem> Check(List<SolutionProject> projects, CommandLineOptions options)
        {
            if (string.IsNullOrEmpty(options.LangVersion))
                yield break;

            foreach (var project in projects)
            {
                var problem = Check(project, options, false);
                if (problem is not null)
                    yield return problem;
            }
        }
    }
}
