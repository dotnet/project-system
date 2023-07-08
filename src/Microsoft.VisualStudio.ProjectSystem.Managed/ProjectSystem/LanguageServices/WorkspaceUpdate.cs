// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Data with which to update a workspace.
/// May come from either evaluation or build, but not both.
/// </summary>
/// <remarks>
/// This is essentially a union type to allow a single dataflow block to
/// handle multiple streams of data in a uniform way.
/// </remarks>
internal sealed class WorkspaceUpdate
{
    public static WorkspaceUpdate FromEvaluation((ConfiguredProject ConfiguredProject, IProjectSnapshot ProjectSnapshot, IProjectSubscriptionUpdate EvaluationRuleUpdate, IProjectSubscriptionUpdate SourceItemsUpdate) update) => new(new(update.ConfiguredProject, update.ProjectSnapshot, update.EvaluationRuleUpdate, update.SourceItemsUpdate), null);

    public static WorkspaceUpdate FromBuild((ConfiguredProject ConfiguredProject, IProjectSubscriptionUpdate BuildRuleUpdate) update) => new(null, new(update.ConfiguredProject, update.BuildRuleUpdate));

    private WorkspaceUpdate(EvaluationUpdate? evaluationUpdate, BuildUpdate? buildUpdate)
    {
        Assumes.True(evaluationUpdate is null ^ buildUpdate is null);

        EvaluationUpdate = evaluationUpdate;
        BuildUpdate = buildUpdate;
    }

    public EvaluationUpdate? EvaluationUpdate { get; }

    public BuildUpdate? BuildUpdate { get; }
}
