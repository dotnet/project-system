// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Groups data related to evaluation updates into a single object.
/// </summary>
internal sealed class EvaluationUpdate
{
    public EvaluationUpdate(ConfiguredProject configuredProject, IProjectSubscriptionUpdate evaluationRuleUpdate, IProjectSubscriptionUpdate sourceItemsUpdate)
    {
        Requires.NotNull(configuredProject, nameof(configuredProject));
        Requires.NotNull(evaluationRuleUpdate, nameof(evaluationRuleUpdate));
        Requires.NotNull(sourceItemsUpdate, nameof(sourceItemsUpdate));

        ConfiguredProject = configuredProject;
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

    public IProjectSubscriptionUpdate EvaluationRuleUpdate { get; }

    public IProjectSubscriptionUpdate SourceItemsUpdate { get; }
}
