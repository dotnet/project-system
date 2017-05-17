// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

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
            var handler = CreateInstance();

            Assert.Throws<ArgumentNullException>("projectChange", () => {
                handler.Handle((IProjectChangeDescription)null, true);
            });
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Initialize(context);
            handler.Handle(added: added, removed: empty, isActiveContext: true);

            Assert.Equal(2, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(added: empty, removed: removed, isActiveContext: true);

            Assert.Equal(1, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Initialize(context);
            handler.Handle(added: added, removed: removed, isActiveContext: true);

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
