// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Trait("UnitTest", "ProjectSystem")]
    public class SourceItemHandlerTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new SourceItemHandler((UnconfiguredProject)null, context);
            });
        }

        [Fact]
        public void Handle1_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle((IComparable)null, projectChange, true, logger);
            });
        }

        [Fact]
        public void Handle1_NullAsProjectChange_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("projectChange", () =>
            {
                handler.Handle(10, (IProjectChangeDescription)null, true, logger);
            });
        }

        [Fact]
        public void Handle1_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, projectChange, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void Handle2_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle((IComparable)null, added, removed, true, logger);
            });
        }

        [Fact]
        public void Handle2_NullAsAdded_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("added", () =>
            {
                handler.Handle(10, (BuildOptions)null, removed, true, logger);
            });
        }

        [Fact]
        public void Handle2_NullAsRemoved_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("removed", () =>
            {
                handler.Handle(10, added, (BuildOptions)null, true, logger);
            });
        }

        [Fact]
        public void Handle2_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, added, removed, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void Handle1_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, projectChange, true, logger);
            });
        }

        [Fact]
        public void Handle2_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, added, removed, true, logger);
            });
        }

        [Fact]
        public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();

            var workspaceContext = IWorkspaceProjectContextFactory.Create();

            handler.Initialize(workspaceContext);

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Initialize(workspaceContext);
            });
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = new SourceItemHandler(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var empty = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: empty, isActiveContext: true, logger: logger);

            AssertEx.CollectionLength(sourceFilesPushedToWorkspace, 2);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            handler.Handle(10, added: empty, removed: removed, isActiveContext: true, logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            void onSourceFileAdded(string s) => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            void onSourceFileRemoved(string s) => sourceFilesPushedToWorkspace.Remove(s);

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForSourceFiles(project, onSourceFileAdded, onSourceFileRemoved);
            var logger = Mock.Of<IProjectLogger>();

            var handler = new SourceItemHandler(project, context);
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null));
            var removed = BuildOptions.FromCommandLineArguments(CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null));

            handler.Handle(10, added: added, removed: removed, isActiveContext: true, logger: logger);

            Assert.Single(sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\ProjectFolder\file1.cs", sourceFilesPushedToWorkspace);
        }

        private SourceItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            return new SourceItemHandler(project, context);
        }
    }
}
