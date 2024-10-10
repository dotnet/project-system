// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Adds a sub-tree beneath the project node in Solution Explorer that shows the setup components
/// required by the project. Enabled via the <see cref="ProjectCapability.DiagnoseVisualStudioComponents"/>
/// project capability.
/// </summary>
[method: ImportingConstructor]
[Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
[AppliesTo(ProjectCapability.DiagnoseVisualStudioComponents)]
internal sealed class VisualStudioComponentSubTreeProvider(
    IProjectThreadingService threadingService,
    UnconfiguredProject unconfiguredProject,
    UnconfiguredSetupComponentDataSource unconfiguredSetupComponentDataSource,
    IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
      : ProjectTreeProviderBase(threadingService, unconfiguredProject),
        IProjectTreeProvider
{
    private IDisposable? _projectCapabilitiesLink;

    protected override void Initialize()
    {
#pragma warning disable RS0030 // Do not use banned APIs
        base.Initialize();
#pragma warning restore RS0030 // Do not use banned APIs

        // IsApplicable may take a project lock, so we can't do it inline with this method
        // which is holding a private lock.  It turns out that doing it asynchronously isn't a problem anyway,
        // so long as we guard against races with the Dispose method.
        _ = unconfiguredProjectTasksService.LoadedProjectAsync(
            async () =>
            {
                await TaskScheduler.Default.SwitchTo(alwaysYield: true);

                UnconfiguredProjectAsynchronousTasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

                lock (SyncObject)
                {
                    Verify.NotDisposed(this);

                    SubmitTreeUpdateAndForget((t, a, c) => Task.FromResult(new TreeUpdateResult(CreateRootNode())));

                    var actionBlock = DataflowBlockFactory.CreateActionBlock(
                        new Action<IProjectVersionedValue<UnconfiguredSetupComponentSnapshot>>(ComponentsChanged),
                        UnconfiguredProject);

                    _projectCapabilitiesLink = unconfiguredSetupComponentDataSource.SourceBlock.LinkTo(
                        actionBlock, DataflowOption.PropagateCompletion);
                }

                JoinUpstreamDataSources(unconfiguredSetupComponentDataSource);
            });

        IProjectTree CreateRootNode()
        {
            return NewTree(
                 caption: "Setup components",
                 icon: KnownMonikers.ViewFront.ToProjectSystemType(),
                 flags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp | ProjectTreeFlags.Common.VirtualFolder));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projectCapabilitiesLink?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ComponentsChanged(IProjectVersionedValue<UnconfiguredSetupComponentSnapshot> e)
    {
        SubmitTreeUpdateAndForget(
            (treeSnapshot, configuredProjectExports, ct) =>
            {
                Assumes.NotNull(treeSnapshot);

                IProjectTree originalTree = treeSnapshot.Value.Tree;
                ImmutableHashSet<string> componentIds = e.Value.ComponentIds;
                IProjectTree tree = originalTree;

                HashSet<string> knownComponents = new(StringComparers.VisualStudioSetupComponentIds);

                // Remove components that are no longer present.
                foreach (IProjectTree existingNode in originalTree.Children)
                {
                    if (!componentIds.Contains(existingNode.Caption))
                    {
                        if (tree.TryFind(existingNode.Identity, out IProjectTree? currentChild))
                        {
                            tree = currentChild.Remove();
                        }
                    }
                    else
                    {
                        knownComponents.Add(existingNode.Caption);
                    }
                }

                foreach (string componentId in componentIds)
                {
                    if (!knownComponents.Contains(componentId))
                    {
                        IProjectTree2 newChild = NewTree(
                            caption: componentId,
                            icon: KnownMonikers.ViewFront.ToProjectSystemType());

                        tree = tree.Add(newChild).Parent!;
                    }
                }

                return Task.FromResult(new TreeUpdateResult(tree, e.DataSourceVersions));
            });
    }

    protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
    {
        return GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
    }

    [Export]
    [method: ImportingConstructor]
    private sealed class MyConfiguredProjectExports(ConfiguredProject configuredProject) : ConfiguredProjectExports(configuredProject)
    {
    }
}
