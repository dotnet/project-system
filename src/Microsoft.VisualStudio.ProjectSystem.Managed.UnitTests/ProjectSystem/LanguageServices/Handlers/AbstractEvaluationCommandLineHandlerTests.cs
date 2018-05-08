// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Logging;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Trait("UnitTest", "ProjectSystem")]
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        [Fact]
        public void ApplyEvaluationChanges_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectLoggerFactory.Create();
            
            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges((IComparable)null, difference, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, (IProjectChangeDiff)null, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsMetadata_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, difference, (ImmutableDictionary<string, IImmutableDictionary<string, string>>)null, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            
            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, difference, metadata, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges((IComparable)null, difference, true, logger);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges(version, (IProjectChangeDiff)null, true, logger);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges(version, difference, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyEvaluationChanges(handler, version, difference);

            Assert.Empty(handler.Files);
        }

        [Fact]
        public void ApplyDesignTimeChanges_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyDesignTimeChanges(handler, version, difference);

            Assert.Empty(handler.Files);
        }

        [Theory]    // Include path                          Expected full path
        [InlineData(@"..\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"..\AnotherProject\Source.cs",         @"C:\AnotherProject\Source.cs")]
        [InlineData(@"Source.cs",                           @"C:\Project\Source.cs")]
        [InlineData(@"C:\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"C:\Project\Source.cs",                @"C:\Project\Source.cs")]
        [InlineData(@"D:\Temp\Source.cs",                   @"D:\Temp\Source.cs")]
        public void ApplyEvaluationChanges_AddsItemFullPathRelativeToProject(string includePath, string expected)
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyEvaluationChanges(handler, 1, difference);

            Assert.Single(handler.Files, expected);
        }

        [Theory]    // Include path                          Expected full path
        [InlineData(@"..\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"..\AnotherProject\Source.cs",         @"C:\AnotherProject\Source.cs")]
        [InlineData(@"Source.cs",                           @"C:\Project\Source.cs")]
        [InlineData(@"C:\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"C:\Project\Source.cs",                @"C:\Project\Source.cs")]
        [InlineData(@"D:\Temp\Source.cs",                   @"D:\Temp\Source.cs")]
        public void ApplyDesignTimeChanges_AddsItemFullPathRelativeToProject(string includePath, string expected)
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyDesignTimeChanges(handler, 1, difference);

            Assert.Single(handler.Files, expected);
        }

        [Theory] // Current state                       Added files                     Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyEvaluationChanges_WithExistingEvaluationChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyEvaluationChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Added files                      Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyDesignTimeChanges_WithExistingEvaluationChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyDesignTimeChanges(handler, 1, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyEvaluationChanges_WithExistingEvaluationChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyEvaluationChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyDesignTimeChanges_WithExistingEvaluationChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyDesignTimeChanges(handler, 1, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                       Added files                     Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyEvaluationChanges_WithExistingDesignTimeChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyEvaluationChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Added files                      Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyDesignTimeChanges_WithExistingDesignTimeChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyDesignTimeChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyEvaluationChanges_WithExistingDesignTimeChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyEvaluationChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyDesignTimeChanges_WithExistingDesignTimeChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyDesignTimeChanges(handler, 2, difference);

            Assert.Equal(handler.Files.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Fact]
        public void ApplyDesignTimeChanges_WhenNewerEvaluationChangesWithAddedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.cs");

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyEvaluationChanges(handler, evaluationVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyDesignTimeChanges(handler, designTimeVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            Assert.Single(handler.Files, @"C:\Project\Source.cs");
        }

        [Fact]
        public void ApplyDesignTimeChanges_WhenNewerEvaluationChangesWithRemovedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.cs");

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyEvaluationChanges(handler, evaluationVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyDesignTimeChanges(handler, designTimeVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            Assert.Empty(handler.Files);
        }

        private static void ApplyEvaluationChanges(AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference)
        {
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            bool isActiveContext = true;
            var logger = IProjectLoggerFactory.Create();

            handler.ApplyEvaluationChanges(version, difference, metadata, isActiveContext, logger);
        }

        private static void ApplyDesignTimeChanges(AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference)
        {
            bool isActiveContext = true;
            var logger = IProjectLoggerFactory.Create();

            handler.ApplyDesignTimeChanges(version, difference, isActiveContext, logger);
        }

        private static EvaluationCommandLineHandler CreateInstanceWithEvaluationItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);

            // Setup the "current state"
            ApplyEvaluationChanges(handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstanceWithDesignTimeItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);

            // Setup the "current state"
            ApplyDesignTimeChanges(handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstance(string fullPath = null)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);

            return new EvaluationCommandLineHandler(project);
        }
    }
}
