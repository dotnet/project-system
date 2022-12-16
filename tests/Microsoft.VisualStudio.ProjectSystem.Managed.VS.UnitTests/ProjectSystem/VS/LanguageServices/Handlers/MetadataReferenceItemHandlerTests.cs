// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class MetadataReferenceItemHandlerTests
    {
        [Fact]
        public void DuplicateMetadataReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onReferenceAdded(string s) => referencesPushedToWorkspace.Add(s);
            void onReferenceRemoved(string s) => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);
            var logger = Mock.Of<IManagedProjectDiagnosticOutputService>();

            var handler = new MetadataReferenceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly2.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(context, 10, added: added, removed: empty, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), logger: logger);

            AssertEx.CollectionLength(referencesPushedToWorkspace, 2);
            Assert.Contains(@"C:\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(context, 10, added: empty, removed: removed, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), logger: logger);

            Assert.Single(referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);
        }

        [Fact]
        public void RootedReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onReferenceAdded(string s) => referencesPushedToWorkspace.Add(s);
            void onReferenceRemoved(string s) => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);
            var logger = Mock.Of<IManagedProjectDiagnosticOutputService>();

            var handler = new MetadataReferenceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:Assembly1.dll", @"/reference:C:\ProjectFolder\Assembly2.dll", @"/reference:..\ProjectFolder\Assembly3.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(context, 10, added: added, removed: removed, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), logger: logger);

            AssertEx.CollectionLength(referencesPushedToWorkspace, 3);
            Assert.Contains(@"C:\ProjectFolder\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly2.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly3.dll", referencesPushedToWorkspace);
        }
    }
}
