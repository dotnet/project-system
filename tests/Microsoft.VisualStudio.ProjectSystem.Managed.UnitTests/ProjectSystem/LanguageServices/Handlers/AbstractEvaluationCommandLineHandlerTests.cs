// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        [Fact]
        public void ApplyProjectEvaluation_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectEvaluation(null!, difference, metadata, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectEvaluation(version, null!, metadata, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_NullAsPreviousMetadata_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var currentMetadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectEvaluation(version, difference, null!, currentMetadata, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_NullAsCurrentMetadata_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var previousMetadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectEvaluation(version, difference, previousMetadata, null!, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectEvaluation(version, difference, metadata, metadata, true, null!);
            });
        }

        [Fact]
        public void ApplyProjectBuild_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectBuild(null!, difference, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectBuild_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectBuild(version, null!, true, logger);
            });
        }

        [Fact]
        public void ApplyProjectBuild_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyProjectBuild(version, difference, true, null!);
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyProjectEvaluation(handler, version, difference);

            Assert.Empty(handler.FileNames);
        }

        [Fact]
        public void ApplyProjectBuild_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyProjectBuild(handler, version, difference);

            Assert.Empty(handler.FileNames);
        }

        [Theory]    // Include path                          Expected full path
        [InlineData(@"..\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"..\AnotherProject\Source.cs",         @"C:\AnotherProject\Source.cs")]
        [InlineData(@"Source.cs",                           @"C:\Project\Source.cs")]
        [InlineData(@"C:\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"C:\Project\Source.cs",                @"C:\Project\Source.cs")]
        [InlineData(@"D:\Temp\Source.cs",                   @"D:\Temp\Source.cs")]
        public void ApplyProjectEvaluation_AddsItemFullPathRelativeToProject(string includePath, string expected)
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyProjectEvaluation(handler, 1, difference);

            Assert.Single(handler.FileNames, expected);
        }

        [Theory]    // Include path                          Expected full path
        [InlineData(@"..\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"..\AnotherProject\Source.cs",         @"C:\AnotherProject\Source.cs")]
        [InlineData(@"Source.cs",                           @"C:\Project\Source.cs")]
        [InlineData(@"C:\Source.cs",                        @"C:\Source.cs")]
        [InlineData(@"C:\Project\Source.cs",                @"C:\Project\Source.cs")]
        [InlineData(@"D:\Temp\Source.cs",                   @"D:\Temp\Source.cs")]
        public void ApplyProjectBuild_AddsItemFullPathRelativeToProject(string includePath, string expected)
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyProjectBuild(handler, 1, difference);

            Assert.Single(handler.FileNames, expected);
        }

        [Theory] // Current state                       Added files                     Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyProjectEvaluation_WithExistingEvaluationChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Fact]
        public void AddEvaluationChanges_CanAddItemWithMetadata()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");

            var difference = IProjectChangeDiffFactory.WithAddedItems("A.cs");
            var metadata = MetadataFactory.Create("A.cs", ("Name", "Value"));

            ApplyProjectEvaluation(handler, 1, difference, metadata);

            var result = handler.Files[@"C:\Project\A.cs"];

            Assert.Equal("Value", result["Name"]);
        }

        [Theory] // Current state                      Added files                      Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyProjectBuild_WithExistingEvaluationChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectBuild(handler, 1, difference);

            Assert.Equal(handler.FileNames.OrderBy(f => f), expectedFiles.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyProjectEvaluation_WithExistingEvaluationChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyProjectBuild_WithExistingEvaluationChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectBuild(handler, 1, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                       Added files                     Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyProjectEvaluation_WithExistingDesignTimeChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Added files                      Expected state
        [InlineData("",                                "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("",                                "A.cs;B.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs",                            "A.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs;C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "D.cs;E.cs;F.cs",                @"C:\Project\A.cs;C:\Project\B.cs;C:\Project\C.cs;C:\Project\D.cs;C:\Project\E.cs;C:\Project\F.cs")]
        public void ApplyProjectBuild_WithExistingDesignTimeChanges_CanAddItem(string currentFiles, string filesToAdd, string expected)
        {
            string[] expectedFiles = expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectBuild(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyProjectEvaluation_WithExistingDesignTimeChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Removed files                    Expected state
        [InlineData("",                                "A.cs",                          @"")]
        [InlineData("",                                "A.cs;B.cs",                     @"")]
        [InlineData("A.cs",                            "A.cs",                          @"")]
        [InlineData("A.cs;B.cs",                       "B.cs",                          @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs;C.cs",                     @"C:\Project\A.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "A.cs;E.cs;F.cs",                @"C:\Project\B.cs;C:\Project\C.cs")]
        public void ApplyProjectBuild_WithExistingDesignTimeChanges_CanRemoveItem(string currentFiles, string filesToRemove, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectBuild(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Original name        New name                         Expected state
        [InlineData("A.cs",                            "A.cs",              "B.cs",                          @"C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",              "C.cs",                          @"C:\Project\A.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "B.cs;C.cs",         "D.cs;E.cs",                     @"C:\Project\A.cs;C:\Project\D.cs;C:\Project\E.cs")]
        [InlineData("A.cs;B.cs",                       "A.cs;B.cs",         "B.cs;A.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        public void ApplyProjectEvaluation_WithExistingEvaluationChanges_CanRenameItem(string currentFiles, string originalNames, string newNames, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRenamedItems(originalNames, newNames);

            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Theory] // Current state                      Original name        New name                         Expected state
        [InlineData("A.cs",                            "A.cs",              "B.cs",                          @"C:\Project\B.cs")]
        [InlineData("A.cs;B.cs",                       "B.cs",              "C.cs",                          @"C:\Project\A.cs;C:\Project\C.cs")]
        [InlineData("A.cs;B.cs;C.cs",                  "B.cs;C.cs",         "D.cs;E.cs",                     @"C:\Project\A.cs;C:\Project\D.cs;C:\Project\E.cs")]
        [InlineData("A.cs;B.cs",                       "A.cs;B.cs",         "B.cs;A.cs",                     @"C:\Project\A.cs;C:\Project\B.cs")]
        public void ApplyProjectEvaluation_WithExistingDesignTimeChanges_CanRenameItem(string currentFiles, string originalNames, string newNames, string expected)
        {
            string[] expectedFiles = expected.Length == 0 ? Array.Empty<string>() : expected.Split(';');

            var handler = CreateInstanceWithDesignTimeItems(@"C:\Project\Project.csproj", currentFiles);

            var difference = IProjectChangeDiffFactory.WithRenamedItems(originalNames, newNames);

            ApplyProjectEvaluation(handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Fact]
        public void ApplyProjectEvaluationChanges_WithExistingEvaluationChanges_CanAddChangeMetadata()
        {
            var file = "A.cs";
            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", file);

            var difference = IProjectChangeDiffFactory.WithChangedItems(file);
            var metadata = MetadataFactory.Create(file, ("Name", "Value"));

            ApplyProjectEvaluation(handler, 2, difference, metadata);

            var result = handler.Files[@"C:\Project\A.cs"];

            Assert.Equal("Value", result["Name"]);
        }

        [Fact]
        public void ApplyProjectBuild_WhenNewerEvaluationChangesWithAddedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyProjectEvaluation(handler, evaluationVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyProjectBuild(handler, designTimeVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            Assert.Single(handler.FileNames, @"C:\Project\Source.cs");
        }

        [Fact]
        public void ApplyProjectBuild_WhenNewerEvaluationChangesWithRemovedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyProjectEvaluation(handler, evaluationVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyProjectBuild(handler, designTimeVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            Assert.Empty(handler.FileNames);
        }

        [Fact]
        public void ApplyProjectBuild_WhenOlderEvaluationChangesWithRemovedConflict_DesignTimeWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");

            int evaluationVersion = 0;

            // Setup the "current state"
            ApplyProjectEvaluation(handler, evaluationVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            int designTimeVersion = 1;

            ApplyProjectBuild(handler, designTimeVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            Assert.Single(handler.FileNames, @"C:\Project\Source.cs");
        }

        private static void ApplyProjectEvaluation(AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference, IImmutableDictionary<string, IImmutableDictionary<string, string>>? metadata = null)
        {
            metadata ??= ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var previousMetadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            bool isActiveContext = true;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            handler.ApplyProjectEvaluation(version, difference, previousMetadata, metadata, isActiveContext, logger);
        }

        private static void ApplyProjectBuild(AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference)
        {
            bool isActiveContext = true;
            var logger = IProjectDiagnosticOutputServiceFactory.Create();

            handler.ApplyProjectBuild(version, difference, isActiveContext, logger);
        }

        private static EvaluationCommandLineHandler CreateInstanceWithEvaluationItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);

            // Setup the "current state"
            ApplyProjectEvaluation(handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstanceWithDesignTimeItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);

            // Setup the "current state"
            ApplyProjectBuild(handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstance(string? fullPath = null)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);

            return new EvaluationCommandLineHandler(project);
        }
    }
}
