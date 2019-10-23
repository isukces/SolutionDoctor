using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.NuGet;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public sealed class Project : IEquatable<Project>
    {
        public static bool operator ==(Project left, Project right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Project left, Project right)
        {
            return !Equals(left, right);
        }

        private static CsProjectKind FindProjectType(XDocument x)
        {
            var root = x?.Root;
            if (root == null) return CsProjectKind.Unknown;
            var isNew = root.Name.LocalName == "Project" && "Microsoft.NET.Sdk" == (string)root.Attribute("Sdk");
            return isNew ? CsProjectKind.New : CsProjectKind.Old;
        }

        public bool Equals(Project other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other)
                   || Equals(Location, other.Location);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Project)obj);
        }

        public override int GetHashCode()
        {
            var tmp = Location;
            return tmp != null ? tmp.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return string.Format("Project {0}", Location);
        }

        private IEnumerable<AssemblyBinding> AssemblyBindingsInternal()
        {
            var configFileInfo = Location.GetAppConfigFile();
            var cfg            = new AppConfig(configFileInfo);

            return cfg.GetAssemblyBindings();
        }

        private List<ProjectReference> GetReferences()
        {
            // <Project Sdk="Microsoft.NET.Sdk">
            var xml    = FileUtils.Load(Location);
            var root   = xml.Root;
            var result = new List<ProjectReference>();
            if (root == null) return result;

            if (Kind == CsProjectKind.New)
            {
                var doc = FileUtils.Load(Location);
                var refNodes = doc?.Root?.Elements("ItemGroup").SelectMany(q => q.Elements("Reference")).ToArray() ??
                               new XElement[0];
                return refNodes.Select(q => { return ProjectReference.FromNode(q, Location.Directory); }).ToList();
            }

            foreach (var itemGroupElement in root.Elements(root.Name.Namespace + "ItemGroup"))
            foreach (var reference in itemGroupElement.Elements(itemGroupElement.Name.Namespace + "Reference"))
                result.Add(ProjectReference.FromNode(reference, Location.Directory));
            return result;
        }

        private NugetPackage[] NugetPackagesInternal()
        {
            if (Kind == CsProjectKind.New)
            {
                /*<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="10.0.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\conexx.translate\conexx.translate.csproj">
      <Project>{794DB2FD-85E2-456E-8DCA-A54EE5C037B9}</Project>
      <Name>conexx.translate</Name>
    </ProjectReference>
  </ItemGroup>
  <ItemGroup>
    <Reference Include="Newtonsoft.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed">
      <HintPath>..\packages\Newtonsoft.Json.10.0.3\lib\net45\Newtonsoft.Json.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>*/
                var doc = FileUtils.Load(Location);
                var refNodes = doc?.Root?.Elements("ItemGroup").SelectMany(q => q.Elements("PackageReference"))
                                   .ToArray() ??
                               new XElement[0];
                return refNodes.Select(q =>
                {
                    var r = new NugetPackage
                    {
                        Id = (string)q.Attribute("Include")
                    };
                    var ver = (string)q.Attribute("Version");

                    if (!string.IsNullOrEmpty(ver))
                        r.Version = NugetVersion.Parse(ver);
                    return r;
                }).ToArray();
            }

            // ReSharper disable once PossibleNullReferenceException
            var configFileInfo = Location.GetPackagesConfigFile();
            if (!configFileInfo.Exists)
                return new NugetPackage[0];
            var xml  = FileUtils.Load(configFileInfo);
            var root = xml.Root;
            if (root == null || root.Name.LocalName != "packages")
                return new NugetPackage[0];
            var packages = root.Elements(root.Name.Namespace + "package");

            return packages.Select(NugetPackage.Parse).ToArray();
        }

        public List<AssemblyBinding> AssemblyBindings
        {
            get { return _assemblyBindings ?? (_assemblyBindings = AssemblyBindingsInternal().ToList()); }
        }

        public NugetPackage[] NugetPackages
        {
            get { return _nugetPackages ?? (_nugetPackages = NugetPackagesInternal()); }
        }

        public List<ProjectReference> References
        {
            get { return _references ?? (_references = GetReferences()); }
        }

        public FileName Location
        {
            get { return _location; }
            set
            {
                if (_location == value)
                    return;
                _location = value;
                Kind = value.Exists
                    ? FindProjectType(FileUtils.Load(value))
                    : CsProjectKind.Unknown;
            }
        }

        public CsProjectKind Kind { get; private set; }

        private List<AssemblyBinding> _assemblyBindings;
        private NugetPackage[] _nugetPackages;
        private List<ProjectReference> _references;
        private FileName _location;
    }

    public enum CsProjectKind
    {
        Unknown,
        Old,
        New
    }
}