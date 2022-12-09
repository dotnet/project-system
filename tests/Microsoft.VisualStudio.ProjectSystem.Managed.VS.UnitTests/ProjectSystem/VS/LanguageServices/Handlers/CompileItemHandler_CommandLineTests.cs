// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class CompileItemHandler_CommandTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new CompileItemHandler(null!);
            });
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IManagedProjectDiagnosticOutputService>();

            var handler = new CompileItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(context, 10, added: added, removed: empty, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), logger: logger);

            AssertEx.CollectionLength(sourceFilesPushedToWorkspace, 2);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(context, 10, added: empty, removed: removed, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextMockFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IManagedProjectDiagnosticOutputService>();

            var handler = new CompileItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(context, 10, added: added, removed: removed, new ContextState(true, false), logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\file1.cs", sourceFilesPushedToWorkspace);
        }
    }
}
