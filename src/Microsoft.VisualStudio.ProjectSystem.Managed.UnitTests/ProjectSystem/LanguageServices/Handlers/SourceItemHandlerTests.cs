using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [ProjectSystemTrait]
    public class SourceItemHandlerTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("project", () => {
                new SourceItemHandler((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void Handle_NullAsProjectChange_ThrowsArgumentNull()
        {
            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var context = IWorkspaceProjectContextFactory.Create();
            
            var handler = CreateInstance();
            
            Assert.Throws<ArgumentNullException>("projectChange", () => {
                handler.Handle(update, (IProjectChangeDescription)null, context, true);
            });
        }


        [Fact]
        public void Handle_NullAsContext_ThrowsArgumentNull()
        {
            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var projectChange = IProjectChangeDescriptionFactory.Create();

            var handler = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () => {
                handler.Handle(update, projectChange, (IWorkspaceProjectContext)null, true);
            });
        }

        [Fact]
        public void OnContextReleasedAsync_NullAsContext_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var context = IProjectChangeDescriptionFactory.Create();

            Assert.Throws<ArgumentNullException>("context", () => {
                handler.OnContextReleasedAsync((IWorkspaceProjectContext)null);
            });
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommonCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommonCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(update, added: added, removed: empty, context: context, isActiveContext: true);

            Assert.Equal(2, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = BuildOptions.FromCommonCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(update, added: empty, removed: removed, context: context, isActiveContext: true);

            Assert.Equal(1, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommonCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommonCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(update, added: added, removed: removed, context: context, isActiveContext: true);

            Assert.Equal(1, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\ProjectFolder\file1.cs", sourceFilesPushedToWorkspace);
        }

        private SourceItemHandler CreateInstance(UnconfiguredProject project = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            return new SourceItemHandler(project);
        }
    }
}
