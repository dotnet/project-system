// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Groups data related to evaluation updates into a single object.
/// </summary>
internal sealed class EvaluationUpdate
{
    public EvaluationUpdate(ConfiguredProject configuredProject, IProjectSnapshot projectSnapshot, IProjectSubscriptionUpdate evaluationRuleUpdate, IProjectSubscriptionUpdate sourceItemsUpdate)
    {
        Requires.NotNull(configuredProject, nameof(configuredProject));
        Requires.NotNull(projectSnapshot, nameof(projectSnapshot));
        Requires.NotNull(evaluationRuleUpdate, nameof(evaluationRuleUpdate));
        Requires.NotNull(sourceItemsUpdate, nameof(sourceItemsUpdate));

        ConfiguredProject = configuredProject;
        ProjectSnapshot = projectSnapshot;
        EvaluationRuleUpdate = evaluationRuleUpdate;
        SourceItemsUpdate = sourceItemsUpdate;
    }

    /// <summary>
    /// Gets the <see cref="ConfiguredProject"/> currently bound to the workspace's project slice.
    /// </summary>
    /// <remarks>
    /// This value may change over time.
    /// For example, it changes when switching between Debug and Release configurations.
    /// </remarks>
    public ConfiguredProject ConfiguredProject { get; }

    /// <summary>
    /// The current MSBuild snapshot of the project. Used only during workspace construction,
    /// in order to allow Roslyn to request arbitrary properties from the project. Without this,
    /// we can only serve up properties defined in our rules, which couples the project system
    /// to Roslyn.
    /// </summary>
    public IProjectSnapshot ProjectSnapshot { get; }

    public IProjectSubscriptionUpdate EvaluationRuleUpdate { get; }

    public IProjectSubscriptionUpdate SourceItemsUpdate { get; }
}
