// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSquigglesTests : AbstractIntegrationTest
    {
        protected override string DefaultLanguageName => LanguageNames.CSharp;

        public CSharpSquigglesTests(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpSquigglesTests), WellKnownProjectTemplates.CSharpNetCoreClassLibrary, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Class1.cs");
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/2281"), Trait("Integration", "Squiggles")]
        public void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    /// <summary/>
    public class Program
    {
        /// <summary/>
        public static void Main(string[] args)
        {
            Console.WriteLine(""Hello World"")
        }

        private void sub()
        {
    }
}");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            var actualTags = VisualStudio.Editor.GetErrorTags();
            var expectedTags = new[]
            {
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[287-288]",
              "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'}'[349-350]",
              "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System.Collections.Generic;\r\nusing System.Text;'[15-68]"
            };
            Assert.Equal(expectedTags, actualTags);
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/2281"), Trait("Integration", "Squiggles")]
        public void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"using System;

class C  : Bar
{
}");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            var actualTags = VisualStudio.Editor.GetErrorTags();
            var expectedTags = new[]
            {
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[29-32]",
                "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'using System;'[0-13]"
            };
            Assert.Equal(expectedTags, actualTags);
        }
    }
}
