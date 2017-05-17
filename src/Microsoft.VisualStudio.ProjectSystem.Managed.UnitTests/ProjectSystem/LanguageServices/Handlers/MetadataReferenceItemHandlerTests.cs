using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [ProjectSystemTrait]
    public class MetadataReferenceItemHandlerTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () => {
                new MetadataReferenceItemHandler((UnconfiguredProject)null, context);
            });
        }

        [Fact]
        public void Constructor_NullAsContext_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();

            Assert.Throws<ArgumentNullException>("context", () => {
                new MetadataReferenceItemHandler(project, (IWorkspaceProjectContext)null);
            });
        }

        [Fact]
        public void DuplicateMetadataReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onReferenceAdded = s => referencesPushedToWorkspace.Add(s);
            Action<string> onReferenceRemoved = s => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);

            var handler = new MetadataReferenceItemHandler(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly2.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: empty, isActiveContext: true);

            Assert.Equal(2, referencesPushedToWorkspace.Count);
            Assert.Contains(@"C:\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(10, added: empty, removed: removed, isActiveContext: true);

            Assert.Equal(1, referencesPushedToWorkspace.Count);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);
        }

        [Fact]
        public void RootedReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onReferenceAdded = s => referencesPushedToWorkspace.Add(s);
            Action<string> onReferenceRemoved = s => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);

            var handler = new MetadataReferenceItemHandler(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:Assembly1.dll", @"/reference:C:\ProjectFolder\Assembly2.dll", @"/reference:..\ProjectFolder\Assembly3.dll" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: removed, isActiveContext: true);

            Assert.Equal(3, referencesPushedToWorkspace.Count);
            Assert.Contains(@"C:\ProjectFolder\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly2.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly3.dll", referencesPushedToWorkspace);
        }
    }
}
