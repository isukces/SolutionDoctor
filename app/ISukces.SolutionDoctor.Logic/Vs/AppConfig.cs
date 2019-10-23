using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Vs
{
    public class AppConfig
    {
        #region Constructors

        public AppConfig([NotNull] FileName fileName)
        {
            _fileName = fileName;
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Exists)
                _xml = FileUtils.Load(fileName);
        }

        #endregion Constructors

        public AssemblyBinding FindByAssemblyIdentity(string id)
        {
            var xAssemblyBinding = GetXAssemblyBinding();
            if (xAssemblyBinding == null)
                return null;
            foreach (var i in GetDependentAssemblyElements(xAssemblyBinding))
            {
                var t = AssemblyBinding.ParseDependentAssembly(i);

                if (t != null && string.Equals(t.Name, id, StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }

        public void Save()
        {
            _xml.Save2(_fileName);
        }

        #region Properties

        public bool Exists
        {
            get { return _xml != null; }
        }

        #endregion Properties

        private readonly FileName _fileName;

        #region Fields

        private readonly XDocument _xml;

        #endregion Fields

        #region Static Methods

        // Private Methods 

        private static XElement[] GetDependentAssemblyElements(XElement xAssemblyBinding)
        {
            if (xAssemblyBinding == null)
                return new XElement[0];
            var dependentAssemblys = xAssemblyBinding
                .Elements(xAssemblyBinding.Name.Namespace + "dependentAssembly")
                .ToArray();
            return dependentAssemblys;
        }

        private static AssemblyBinding[] ParseAssemblyBinding(XElement xAssemblyBinding)
        {
            var dependentAssemblyElements = GetDependentAssemblyElements(xAssemblyBinding);
            return dependentAssemblyElements
                .Select(AssemblyBinding.ParseDependentAssembly)
                .Where(a => a != null)
                .ToArray();
        }

        #endregion Static Methods

        #region Methods

        // Public Methods 

        public IEnumerable<AssemblyBinding> GetAssemblyBindings()
        {
            var xAssemblyBinding = GetXAssemblyBinding();
            return xAssemblyBinding == null
                ? new AssemblyBinding[0]
                : ParseAssemblyBinding(xAssemblyBinding);
        }
        // Private Methods 

        private XElement GetXAssemblyBinding()
        {
            if (_xml == null) return null;
            var root = _xml.Root;
            if (root == null || root.Name.LocalName != "configuration")
                return null;
            var runtime = root.Element(root.Name.Namespace + "runtime");
            if (runtime == null)
                return null;
            var ab = runtime.Elements().SingleOrDefault(a => a.Name.LocalName == "assemblyBinding");
            return ab;
        }

        #endregion Methods
    }
}