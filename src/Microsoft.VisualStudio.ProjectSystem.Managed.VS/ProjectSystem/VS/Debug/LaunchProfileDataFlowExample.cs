// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <remarks>
/// An example of a very simple--but realistic and complete--use of data flows: consuming an existing data flow so
/// we can detect changes to the launch settings, and update other parts of the system as a result.
/// 
/// We're going to hook up to data flow exposed by ILaunchSettingsProvider.SourceBlock. The ILaunchSettingsProvider
/// is only available when the project has the "LaunchProfiles" capability. While this particular capability does
/// not tend to change while a project is loaded, in general capabilities can change and we need a way to react to
/// that. Enter the IProjectDynamicLoadComponent. Implementations of this interface are "loaded" whenever their
/// capabilities are satisfied, and "unloaded" whenever the capabilities are no longer satisfied. So here we export
/// this as an IProjectDynamicLoadComponent, and use the AppliesTo attibute to tie it to the LaunchProfiles
/// capability.
/// 
/// Note we specify ExportContractNames.Scopes.UnconfiguredProject in the Export attribute because we expect/need
/// one instance of this type per UnconfiguredProject.
/// </remarks>
[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapabilities.LaunchProfiles)]
internal class LaunchProfileDataFlowExample : IProjectDynamicLoadComponent
{
    /// <remarks>
    /// We need the UnconfiguredProject for several reasons:
    ///   1. We need the full path to the project file so we can edit the properties in that file when the launch
    ///      settings change.
    ///   2. It provides context for fault handling. If this type throws an exception while processing the data from
    ///      the data flow, the project system can pop up a gold bar blaming the affected project and letting the
    ///      user know that things may not work properly.
    ///   3. Importing an UnconfiguredProject in the constructor also guarantees we get a new instance of this type
    ///      associated with each UnconfiguredProject (though this is a bit redundant as the other two imports would
    ///      also cause this).
    /// </remarks>
    private readonly UnconfiguredProject _project;
    /// <remarks>
    /// We need the ILaunchSettingsProvider in order to hook up to ILaunchSettingsProvider.SourceBlock.
    /// </remarks>
    private readonly ILaunchSettingsProvider _launchSettingsProvider;
    /// <remarks>
    /// The IProjectPropertiesProvider is needed to update the project properties when the launch settings change.
    /// </remarks>
    private readonly IProjectPropertiesProvider _projectProperties;

    /// <remarks>
    /// Hooking up to a data flow source creates a link; disposing the link is how to break the connection to the data
    /// flow source when we no longer need it.
    /// </remarks>
    private IDisposable? _launchSettingsLink;
    /// <remarks>
    /// We need to store some state, namely, the previous command name/debug target.
    /// </remarks>
    private string? _previousCommandName = null;

    [ImportingConstructor]
    public LaunchProfileDataFlowExample(
        UnconfiguredProject unconfiguredProject,
        ILaunchSettingsProvider launchSettingsProvider,
        // There are multiple implementations of IProjectPropertiesProvider. The Import attribute here tells MEF to give
        // us the specific one named "ProjectFile".
        [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider projectProperties)
    {
        _project = unconfiguredProject;
        _launchSettingsProvider = launchSettingsProvider;
        _projectProperties = projectProperties;
    }

    /// <remarks>
    /// IProjectDynamicLoadComponent.LoadAsync is called every time the project satisifies the capabilities specified via
    /// AppliesTo. This is where we hook up to the data flow source.
    /// </remarks>
    public Task LoadAsync()
    {
        // This is a defensive null check. It shouldn't be necessary, but prevents us from hooking up to the same data flow
        // multiple times and, more importantly, losing track of links that such that we can no longer break them.
        if (_launchSettingsLink is null)
        {
            // Here we use the DataflowUtilities.LinkToAsyncAction extension method to connect the data flow to the
            // OnLaunchSettingsChangesAsync method, specifying the UnconfiguredProject as the "context" for the purposes of
            // error reporting.
            _launchSettingsLink = _launchSettingsProvider.SourceBlock
                .LinkToAsyncAction(OnLaunchSettingsChangedAsync, _project);
        }

        return Task.CompletedTask;
    }

    /// <remarks>
    /// IProjectDynamicLoadComponent.UnloadAsync is called every time the project ceases to satisfy the capabilities specified 
    /// via AppliesTo. This is where we break the link to the data flow source.
    /// </remarks>
    public Task UnloadAsync()
    {
        // This is a defensive null check. It shouldn't be necessary, but prevents us from trying to call Dispose() on a null.
        if (_launchSettingsLink is not null)
        {
            _launchSettingsLink.Dispose();
            _launchSettingsLink = null;
            _previousCommandName = null;
        }

        return Task.CompletedTask;
    }

    /// <remarks>
    /// Called every time a new set of ILaunchSettings are pushed through the data flow pipeline.
    /// 
    /// Note that setting a project property will cause us to enter the project system's write lock for the project while we
    /// are part of data flow processing. Mixing data flow and locking in this way is _generally_ a bad idea (in particular it
    /// is not safe to wait for data to come through a data flow while holding a read or write lock) but is probably OK here
    /// since the launch settings aren't actually covered by the read or write locks as they are not part of the project's
    /// MSBuild representation. If the data flow exposed MSBuild data, however, we would make the change to the project
    /// properties in a separate "fire-and-forget" task that dropped any ambient state (such the locks).
    /// </remarks>
    private async Task OnLaunchSettingsChangedAsync(ILaunchSettings arg)
    {
        // If the "command name" associated with the active launch profile has changed...
        if (_previousCommandName is null
            || !StringComparers.LaunchProfileCommandNames.Equals(arg?.ActiveProfile?.CommandName, _previousCommandName))
        {
            _previousCommandName = arg?.ActiveProfile?.CommandName;
            // ... then get the project properties for the project file (specifying null for itemType and item to indicate that
            // we want _project_ properties rather than _item_ metadata)...
            IProjectProperties properties = _projectProperties.GetProperties(_project.FullPath, itemType: null, item: null);

            // ... and then update a property value in the project file.

            await properties.SetPropertyValueAsync("CurrentCommandName", _previousCommandName ?? "");
        }
    }
}
