﻿using System;
using System.Xml.Linq;
using ISukces.SolutionDoctor.Logic.Problems;

namespace ISukces.SolutionDoctor.Logic.Checkers.Xaml
{
    internal class XamlNotInPageNodeProblem : XamlProblem
    {
        public override void Describe(Action<string> writeLine)
        {
            writeLine($"File {XamlFile} should be marked as Page");
        }

     
        protected override void FixNode(CsprojXmlNodeWrapper wrapper)
        {
            wrapper.WrappedElement.Name = wrapper.WrappedElement.Name.Namespace + "Page";
        }

        protected override string GetFixName()
        {
            return $"Set {XamlFile} as page";
        }

        protected override bool GetIsBigProblem()
        {
            return true;
        }

        public string DependendUppon { get; set; }
    }
}