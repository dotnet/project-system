using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [ProjectSystemTrait]
    public class MetadataReferenceItemHandlerTests
    {
        [Fact]
        public void Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new MetadataReferenceItemHandler(project: null));
            new MetadataReferenceItemHandler(UnconfiguredProjectFactory.Create());
        }

        [Fact]
        public void DuplicateMetadataReferencesPushedToWorkspace()
        {
            var referencesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onReferenceAdded = s => referencesPushedToWorkspace.Add(s);
            Action<string> onReferenceRemoved = s => referencesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForMetadataReferences(project, onReferenceAdded, onReferenceRemoved);

            var handler = new MetadataReferenceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly2.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null);
            var empty = CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null);

            handler.Handle(added: added, removed: empty, context: context, isActiveContext: true);

            Assert.Equal(2, referencesPushedToWorkspace.Count);
            Assert.Contains(@"C:\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\Assembly2.dll", referencesPushedToWorkspace);

            var removed = CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:C:\Assembly1.dll", @"/reference:C:\Assembly1.dll" }, baseDirectory: projectDir, sdkDirectory: null);
            handler.Handle(added: empty, removed: removed, context: context, isActiveContext: true);

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

            var handler = new MetadataReferenceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = CSharpCommandLineParser.Default.Parse(args: new[] { @"/reference:Assembly1.dll", @"/reference:C:\ProjectFolder\Assembly2.dll", @"/reference:..\ProjectFolder\Assembly2.dll" }, baseDirectory: projectDir, sdkDirectory: null);
            var removed = CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null);

            handler.Handle(added: added, removed: removed, context: context, isActiveContext: true);

            Assert.Equal(2, referencesPushedToWorkspace.Count);
            Assert.Contains(@"C:\ProjectFolder\Assembly1.dll", referencesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\Assembly2.dll", referencesPushedToWorkspace);
        }
    }
}
