// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    [Trait("Integration", "Squiggles")]
    public class VisualBasicSquigglesTest : AbstractIntegrationTest
    {
        protected override string DefaultLanguageName => LanguageNames.VisualBasic;

        public VisualBasicSquigglesTest(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpSquigglesTests), WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Class1.vb");
        }

        [Fact(Skip = "Syntax squiggles not showing on VB")]
        public void VerifySyntaxErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S()
        Dim x = 1 +
      End Sub
End Class");

            VisualStudio.WaitForApplicationIdle();
            var actualTags = VisualStudio.Editor.GetErrorTags();
            var expectedTags = new[]
            {
               "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\r'[43-44]",
            };

            actualTags.ShouldEqualWithDiff(expectedTags);
        }

        [Fact]
        public void VerifySemanticErrorSquiggles()
        {
            VisualStudio.Editor.SetText(@"Class A
      Sub S(b as Bar)
      End Sub
End Class");
            VisualStudio.Editor.Verify.ErrorTags("Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[26-29]");
        }
    }
}
