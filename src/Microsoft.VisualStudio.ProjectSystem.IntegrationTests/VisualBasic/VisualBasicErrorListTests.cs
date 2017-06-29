// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class VisualBasicErrorListTests : AbstractIntegrationTest
    {
        protected override string DefaultLanuageName => LanguageNames.VisualBasic;

        public VisualBasicErrorListTests(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpSquigglesTests), WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Class1.vb");
        }

        [Fact(Skip = "https://github.com/dotnet/project-system/issues/2281"), Trait("Integration", "ErrorList")]
        public void ErrorList()
        {
            VisualStudio.Editor.SetText(@"
Module Module1

    Function Food() As P
        Return Nothing
    End Function

    Sub Main()
        Foo()
    End Sub

End Module
");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "Type 'P' is not defined.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 4,
                    column: 24),
                new ErrorListItem(
                    severity: "Error",
                    description: "'Foo' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 9,
                    column: 9)
            };
            VisualStudio.WaitForApplicationIdle();
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
            VisualStudio.ErrorList.NavigateToErrorListItem(0);
            VisualStudio.Editor.Verify.CaretPosition(43);
        }

        [Fact, Trait("Integration", "ErrorList")]
        public void ErrorsDuringMethodBodyEditing()
        {
            VisualStudio.Editor.SetText(@"
Namespace N
    Class C
        Private F As Integer
        Sub S()
             ' Comment
        End Sub
    End Class
End Namespace
");
            VisualStudio.Editor.PlaceCaret(" Comment", charsOffset: -2);
            VisualStudio.SendKeys.Send("F = 0");
            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new ErrorListItem[] { };
            var actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("F = 0 ' Comment", charsOffset: -1);
            VisualStudio.Editor.SendKeys("F");
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "'FF' is not declared. It may be inaccessible due to its protection level.",
                    project: "TestProj.vbproj",
                    fileName: "Class1.vb",
                    line: 6,
                    column: 13)
            };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);

            VisualStudio.Editor.Activate();
            VisualStudio.Editor.PlaceCaret("FF = 0 ' Comment", charsOffset: -1);
            VisualStudio.Editor.SendKeys(VirtualKey.Delete);
            VisualStudio.ErrorList.ShowErrorList();
            expectedContents = new ErrorListItem[] { };
            actualContents = VisualStudio.ErrorList.GetErrorListContents();
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
