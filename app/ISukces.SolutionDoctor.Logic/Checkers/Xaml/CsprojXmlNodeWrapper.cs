using System.Xml.Linq;
using JetBrains.Annotations;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    public struct CsprojXmlNodeWrapper
    {
        public CsprojXmlNodeWrapper(XElement wrappedElement)
        {
            WrappedElement = wrappedElement;
        }

        public bool HasIncludeExtension(string ext)
        {
            return Include.ToLower().EndsWith(ext.ToLower());
        }

        private string GetNestedNodeValue(string nodeName)
        {
            return WrappedElement.Element(WrappedElement.Name.Namespace + nodeName)?.Value ??
                   string.Empty;
        }

        private void SetNestedNodeValue(string nodeName, string value)
        {
            var current = WrappedElement.Element(WrappedElement.Name.Namespace + nodeName);
            if (current != null)
                current.Value = value;
            else
                WrappedElement.Add(new XElement(WrappedElement.Name.Namespace + nodeName, value));
        }

        public XElement WrappedElement { get; }

        [NotNull]
        public string Include => (string)WrappedElement.Attribute("Include") ?? string.Empty;


        [NotNull]
        public string DependentUpon
        {
            get => GetNestedNodeValue("DependentUpon");
            set => SetNestedNodeValue("DependentUpon", value);
        }

        [NotNull]
        public string SubType
        {
            get => GetNestedNodeValue("SubType");
            set => SetNestedNodeValue("SubType", value);
        }

        [NotNull]
        public string Generator
        {
            get => GetNestedNodeValue("Generator");
            set => SetNestedNodeValue("Generator", value);
        }

        public NodeType NodeType
        {
            get
            {
                switch (WrappedElement.Name.LocalName)
                {
                    case "None":
                        return NodeType.None;
                    case "Reference":
                        return NodeType.Reference;
                    case "Page":
                        return NodeType.Page;
                    case "Content":
                        return NodeType.Content;
                }

                return NodeType.Unknown;
            }
        }
    }

    public enum NodeType
    {
        Unknown,
        None,
        Page,
        Reference,
        Content
    }
}