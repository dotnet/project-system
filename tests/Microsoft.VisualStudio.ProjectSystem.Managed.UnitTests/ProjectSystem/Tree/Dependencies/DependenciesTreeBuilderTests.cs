// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

public sealed class DependenciesTreeBuilderTests
{
    // TODO validate rules / IDependencyWithBrowseObject
    // TODO validate unconfigured dependencies
    // TODO validate when tree items should not be changed (via CustomTag)

    private readonly ProjectConfigurationSlice _slice = ProjectConfigurationSlice.Create(ImmutableDictionary<string, string>.Empty.Add("TargetFramework", "net8.0"));
    private readonly ProjectConfigurationSlice _slice2 = ProjectConfigurationSlice.Create(ImmutableDictionary<string, string>.Empty.Add("TargetFramework", "net7.0"));

    private readonly DependenciesTreeBuilder _builder;
    private readonly ITestOutputHelper _output;

    [Fact]
    public async Task BuildTreeAsync_EmptySnapshot_ReturnsOriginalTree()
    {
        var snapshot = CreateDependenciesSnapshot();

        var original = new TestProjectTree();

        var tree = await _builder.BuildTreeAsync(original, snapshot, CancellationToken.None);

        Assert.Same(original, tree);
    }

    [Fact]
    public async Task BuildTreeAsync_EmptySnapshot_CreatesTreeWhenNonePrior()
    {
        var snapshot = CreateDependenciesSnapshot();

        var tree = await _builder.BuildTreeAsync(null, snapshot, CancellationToken.None);

        ValidateTree(
            tree,
            """
            Caption=Dependencies, Icon=ReferenceGroup, ExpandedIcon=ReferenceGroup, IsProjectItem=False, BubbleUpFlag=True
            """);
    }

    [Fact]
    public async Task BuildTreeAsync_ReturnsOriginalTreeWhenCancelled()
    {
        var snapshot = CreateDependenciesSnapshot(new() { [_slice] = new() { [DependencyGroupTypes.Packages] = new[] { new MockDependency { Id = "Dependency1", Caption = "Caption1" } } } });

        var original = new TestProjectTree();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var tree = await _builder.BuildTreeAsync(original, snapshot, cts.Token);

        Assert.Same(original, tree);
    }

    [Fact]
    public async Task BuildTreeAsync_SingleSlice_SingleDependency()
    {
        var snapshot = CreateDependenciesSnapshot(new() { [_slice] = new() { [DependencyGroupTypes.Packages] = new[] { new MockDependency { Id = "Dependency1", Caption = "Caption1" } } } });

        var tree = await _builder.BuildTreeAsync(null, snapshot, CancellationToken.None);

        ValidateTree(
            tree,
            """
            Caption=Dependencies, Icon=ReferenceGroup, ExpandedIcon=ReferenceGroup, IsProjectItem=False, BubbleUpFlag=True
                Caption=Packages, Icon=NuGetNoColor, IsProjectItem=False
                    Caption=Caption1, Icon=Reference, IsProjectItem=True
            """);
    }

    [Fact]
    public async Task BuildTreeAsync_SingleSlice_SingleDependency_DiagnosticUpSpine()
    {
        var snapshot = CreateDependenciesSnapshot(new() { [_slice] = new() { [DependencyGroupTypes.Packages] = new[] { new MockDependency { Id = "Dependency1", Caption = "Caption1", DiagnosticLevel = DiagnosticLevel.Warning, Icon = KnownProjectImageMonikers.NuGetNoColorWarning } } } });

        var tree = await _builder.BuildTreeAsync(null, snapshot, CancellationToken.None);

        ValidateTree(
            tree,
            """
            Caption=Dependencies, Icon=ReferenceGroupWarning, ExpandedIcon=ReferenceGroupWarning, IsProjectItem=False, BubbleUpFlag=True
                Caption=Packages, Icon=NuGetNoColorWarning, IsProjectItem=False
                    Caption=Caption1, Icon=NuGetNoColorWarning, IsProjectItem=True
            """);
    }

    [Fact]
    public async Task BuildTreeAsync_MultipleSlices_SingleDependencyInEach()
    {
        var snapshot = CreateDependenciesSnapshot(new()
        {
            [_slice]  = new() { [DependencyGroupTypes.Packages] = new[] { new MockDependency { Id = "Dependency1", Caption = "Caption1" } } },
            [_slice2] = new() { [DependencyGroupTypes.Packages] = new[] { new MockDependency { Id = "Dependency2", Caption = "Caption2" } } }
        });

        var tree = await _builder.BuildTreeAsync(null, snapshot, CancellationToken.None);

        ValidateTree(
            tree,
            """
            Caption=Dependencies, Icon=ReferenceGroup, ExpandedIcon=ReferenceGroup, IsProjectItem=False, BubbleUpFlag=True
                Caption=net8.0, Icon=Library, IsProjectItem=False
                    Caption=Packages, Icon=NuGetNoColor, IsProjectItem=False
                        Caption=Caption1, Icon=Reference, IsProjectItem=True
                Caption=net7.0, Icon=Library, IsProjectItem=False
                    Caption=Packages, Icon=NuGetNoColor, IsProjectItem=False
                        Caption=Caption2, Icon=Reference, IsProjectItem=False
            """);
    }

    [Fact]
    public async Task BuildTreeAsync_SingleSlice_EmptyDependenciesCollection_ShowsEmptyGroupNode()
    {
        var snapshot = CreateDependenciesSnapshot(new() { [_slice] = new() { [DependencyGroupTypes.Packages] = Array.Empty<IDependency>() } });

        var tree = await _builder.BuildTreeAsync(null, snapshot, CancellationToken.None);

        ValidateTree(
            tree,
            """
            Caption=Dependencies, Icon=ReferenceGroup, ExpandedIcon=ReferenceGroup, IsProjectItem=False, BubbleUpFlag=True
                Caption=Packages, Icon=NuGetNoColor, IsProjectItem=False
            """);
    }

    #region Test support

    public DependenciesTreeBuilderTests(ITestOutputHelper output)
    {
        _output = output;

        UnconfiguredProject unconfiguredProject = UnconfiguredProjectFactory.Create();
        IProjectThreadingService threadingService = IProjectThreadingServiceFactory.Create();
        IUnconfiguredProjectTasksService tasksService = IUnconfiguredProjectTasksServiceFactory.Create();
        IProjectAccessor projectAccessor = IProjectAccessorFactory.Create();

        var treeConstruction = new Mock<IProjectTreeOperations>(MockBehavior.Strict);
        treeConstruction.Setup(o => o.NewTree(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<IRule?>(),
            It.IsAny<ProjectImageMoniker?>(),
            It.IsAny<ProjectImageMoniker?>(),
            It.IsAny<bool>(),
            It.IsAny<ProjectTreeFlags?>(),
            It.IsAny<int>()
            )).Returns((string caption, string? filePath, IRule? _, ProjectImageMoniker? icon, ProjectImageMoniker? _, bool _, ProjectTreeFlags? flags, int _)
                => new TestProjectTree { Caption = caption, FilePath = filePath, Icon = icon, Flags = flags ?? ProjectTreeFlags.Empty, IsProjectItem = false });

        treeConstruction.Setup(o => o.NewTree(
            It.IsAny<string>(),
            It.IsAny<IProjectPropertiesContext>(),
            It.IsAny<IPropertySheet?>(),
            It.IsAny<IRule?>(),
            It.IsAny<ProjectImageMoniker?>(),
            It.IsAny<ProjectImageMoniker?>(),
            It.IsAny<bool>(),
            It.IsAny<ProjectTreeFlags?>(),
            It.IsAny<bool>(),
            It.IsAny<int>()
            )).Returns((
                string caption,
                IProjectPropertiesContext _,
                IPropertySheet? _,
                IRule? _,
                ProjectImageMoniker? icon,
                ProjectImageMoniker? _,
                bool _,
                ProjectTreeFlags? flags,
                bool _,
                int _)
                => new TestProjectTree { Caption = caption, Icon = icon, Flags = flags ?? ProjectTreeFlags.Empty, IsProjectItem = true });

        _builder = new DependenciesTreeBuilder(unconfiguredProject)
        {
            TreeConstruction = treeConstruction.Object
        };
    }

    private DependenciesSnapshot CreateDependenciesSnapshot(Dictionary<ProjectConfigurationSlice, Dictionary<DependencyGroupType, IDependency[]>>? configuredDependencies = null)
    {
        configuredDependencies ??= new() { [_slice] = new() };

        var dependenciesBySlice = configuredDependencies.ToImmutableDictionary(
            pair => pair.Key,
            pair => DependenciesSnapshotSliceFactory.Create(
                slice: pair.Key,
                dependenciesByType: pair.Value.ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableArray())));

        var unconfiguredDependenciesByType = ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>>.Empty;

        return new DependenciesSnapshot(_slice, dependenciesBySlice, unconfiguredDependenciesByType);
    }

    private void ValidateTree(IProjectTree resultTree, string expected)
    {
        string actual = ToTestDataString((TestProjectTree)resultTree);

        if (expected != actual)
        {
            _output.WriteLine("EXPECTED");
            _output.WriteLine(expected);
            _output.WriteLine("ACTUAL");
            _output.WriteLine(actual);
        }

        Assert.Equal(expected, actual);

        return;

        static string ToTestDataString(TestProjectTree root)
        {
            var builder = new StringBuilder();

            GetChildrenTestStats(root, indent: 0);

            return builder.ToString();

            void GetChildrenTestStats(TestProjectTree tree, int indent)
            {
                WriteLine();

                foreach (var child in tree.Children)
                {
                    builder.AppendLine();
                    GetChildrenTestStats(child, indent + 1);
                }

                void WriteLine()
                {
                    builder.Append(' ', indent * 4);
                    builder.Append("Caption=").Append(tree.Caption);
                    builder.Append(", Icon=").Append(GetIconName(tree.Icon));

                    if (tree.ExpandedIcon is not null)
                    {
                        builder.Append(", ExpandedIcon=").Append(GetIconName(tree.ExpandedIcon));
                    }

                    builder.Append(", IsProjectItem=").Append(tree.IsProjectItem);

                    if (tree.BrowseObjectProperties?.Name is string rule)
                    {
                        builder.Append(", Rule=").Append(rule);
                    }

                    if (tree.CustomTag is not null)
                    {
                        builder.Append(", CustomTag=").Append(tree.CustomTag);
                    }

                    if (tree.Flags.Contains(ProjectTreeFlags.Common.BubbleUp))
                    {
                        builder.Append(", BubbleUpFlag=True");
                    }

                    static string GetIconName(ProjectImageMoniker? icon)
                    {
                        return icon is null ? "" : ImageMonikerDebuggerDisplay.FromProjectImageMoniker(icon).Replace("KnownImageIds.", "");
                    }
                }
            }
        }
    }

    private sealed class MockDependency : IDependency
    {
        public required string Id { get; init; }
        public required string Caption { get; init; }
        public ProjectImageMoniker Icon { get; init; } = KnownProjectImageMonikers.Reference;
        public ProjectTreeFlags Flags { get; init; } = DependencyTreeFlags.Dependency;
        public DiagnosticLevel DiagnosticLevel { get; init; } = DiagnosticLevel.None;
    }

    #endregion
}
