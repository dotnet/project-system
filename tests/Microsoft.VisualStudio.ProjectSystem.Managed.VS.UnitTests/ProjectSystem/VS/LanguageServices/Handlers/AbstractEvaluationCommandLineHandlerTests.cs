// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        [Fact]
        public void ApplyProjectEvaluation_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();
            var context = IWorkspaceProjectContextMockFactory.Create();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyProjectEvaluation(context, handler, version, difference);

            Assert.Empty(handler.FileNames);
        }

        [Fact]
        public void ApplyProjectBuild_WhenNoChanges_DoesNothing()
        {
            var handler = CreateInstance();
            var context = IWorkspaceProjectContextMockFactory.Create();

            var version = 1;
            var difference = IProjectChangeDiffFactory.WithNoChanges();

            ApplyProjectBuild(context, handler, version, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyProjectEvaluation(context, handler, 1, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();
            var difference = IProjectChangeDiffFactory.WithAddedItems(includePath);

            ApplyProjectBuild(context, handler, 1, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectEvaluation(context, handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Fact]
        public void AddEvaluationChanges_CanAddItemWithMetadata()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems("A.cs");
            var metadata = MetadataFactory.Create("A.cs", ("Name", "Value"));

            ApplyProjectEvaluation(context, handler, 1, difference, metadata);

            var result = handler.Files[@"C:\Project\A.cs"];

            Assert.Equal("Value", result["Name"]);
        }

        [Fact]
        public void AddEvaluationChanges_ItemsWithExclusionMetadataAreIgnored()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems("A.cs;B.cs;C.cs");
            var metadata = MetadataFactory.Create("A.cs", ("ExcludeFromCurrentConfiguration", "true"))
                                          .Add("B.cs", ("ExcludeFromCurrentConfiguration", "false"));
                            

            ApplyProjectEvaluation(context, handler, 1, difference, metadata);

            string[] expectedFiles = new[] { @"C:\Project\B.cs", @"C:\Project\C.cs" };
            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectBuild(context, handler, 1, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectEvaluation(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectBuild(context, handler, 1, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectEvaluation(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithAddedItems(filesToAdd);
            ApplyProjectBuild(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectEvaluation(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRemovedItems(filesToRemove);
            ApplyProjectBuild(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRenamedItems(originalNames, newNames);

            ApplyProjectEvaluation(context, handler, 2, difference);

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
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithRenamedItems(originalNames, newNames);

            ApplyProjectEvaluation(context, handler, 2, difference);

            Assert.Equal(expectedFiles.OrderBy(f => f), handler.FileNames.OrderBy(f => f));
        }

        [Fact]
        public void ApplyProjectEvaluationChanges_WithExistingEvaluationChanges_CanAddChangeMetadata()
        {
            var file = "A.cs";
            var handler = CreateInstanceWithEvaluationItems(@"C:\Project\Project.csproj", file);
            var context = IWorkspaceProjectContextMockFactory.Create();

            var difference = IProjectChangeDiffFactory.WithChangedItems(file);
            var metadata = MetadataFactory.Create(file, ("Name", "Value"));

            ApplyProjectEvaluation(context, handler, 2, difference, metadata);

            var result = handler.Files[@"C:\Project\A.cs"];

            Assert.Equal("Value", result["Name"]);
        }

        [Fact]
        public void ApplyProjectBuild_WhenNewerEvaluationChangesWithAddedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var context = IWorkspaceProjectContextMockFactory.Create();

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyProjectEvaluation(context, handler, evaluationVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyProjectBuild(context, handler, designTimeVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            Assert.Single(handler.FileNames, @"C:\Project\Source.cs");
        }

        [Fact]
        public void ApplyProjectBuild_WhenNewerEvaluationChangesWithRemovedConflict_EvaluationWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var context = IWorkspaceProjectContextMockFactory.Create();

            int evaluationVersion = 1;

            // Setup the "current state"
            ApplyProjectEvaluation(context, handler, evaluationVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            int designTimeVersion = 0;

            ApplyProjectBuild(context, handler, designTimeVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            Assert.Empty(handler.FileNames);
        }

        [Fact]
        public void ApplyProjectBuild_WhenOlderEvaluationChangesWithRemovedConflict_DesignTimeWinsOut()
        {
            var handler = CreateInstance(@"C:\Project\Project.csproj");
            var context = IWorkspaceProjectContextMockFactory.Create();

            int evaluationVersion = 0;

            // Setup the "current state"
            ApplyProjectEvaluation(context, handler, evaluationVersion, IProjectChangeDiffFactory.WithRemovedItems("Source.cs"));

            int designTimeVersion = 1;

            ApplyProjectBuild(context, handler, designTimeVersion, IProjectChangeDiffFactory.WithAddedItems("Source.cs"));

            Assert.Single(handler.FileNames, @"C:\Project\Source.cs");
        }

        private static void ApplyProjectEvaluation(IWorkspaceProjectContext context, AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference, IImmutableDictionary<string, IImmutableDictionary<string, string>>? metadata = null)
        {
            metadata ??= ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var previousMetadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            bool isActiveContext = true;
            var logger = IManagedProjectDiagnosticOutputServiceFactory.Create();

            handler.ApplyProjectEvaluation(context, version, difference, previousMetadata, metadata, isActiveContext, logger);
        }

        private static void ApplyProjectBuild(IWorkspaceProjectContext context, AbstractEvaluationCommandLineHandler handler, IComparable version, IProjectChangeDiff difference)
        {
            bool isActiveContext = true;
            var logger = IManagedProjectDiagnosticOutputServiceFactory.Create();

            handler.ApplyProjectBuild(context, version, difference, isActiveContext, logger);
        }

        private static EvaluationCommandLineHandler CreateInstanceWithEvaluationItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);
            var context = IWorkspaceProjectContextMockFactory.Create();

            // Setup the "current state"
            ApplyProjectEvaluation(context, handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstanceWithDesignTimeItems(string fullPath, string semiColonSeparatedItems)
        {
            var handler = CreateInstance(fullPath);
            var context = IWorkspaceProjectContextMockFactory.Create();

            // Setup the "current state"
            ApplyProjectBuild(context, handler, 1, IProjectChangeDiffFactory.WithAddedItems(semiColonSeparatedItems));

            return handler;
        }

        private static EvaluationCommandLineHandler CreateInstance(string? fullPath = null)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);

            return new EvaluationCommandLineHandler(project);
        }
    }
}
