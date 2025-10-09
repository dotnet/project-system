// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.System, Cardinality = ImportCardinality.ExactlyOne)]
public interface ICssHotReloadService
{
    /// <summary>
    /// Returns the name of this projects scoped css filename.
    /// </summary>
    string GetScopedCSSFilenameForProject();

    /// <summary>
    /// Once this component is loaded for a project it will remain active even if a capability is removed and the feature
    /// should be disabled. This property defaults to true. Set to false if the capability supporting this feature
    /// is removed.
    /// </summary>
    bool CssHotReloadEnabled { get; set; }

    ValueTask<HotReloadResult> ApplyUpdatesAsync(CancellationToken cancellationToken);
}
