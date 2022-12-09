// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

public class WorkspaceFactoryTests
{
    [Fact]
    public async Task WorkspaceUpdateOrderingBlock_UsualSequence()
    {
        var u1 = EvaluationUpdate();
        var u2 = BuildUpdate();
        var u3 = EvaluationUpdate();
        var u4 = BuildUpdate();

        await VerifyOrderAsync(
            inputs:  new[] { u1, u2, u3, u4 },
            outputs: new[] { u1, u2, u3, u4 }); // sequence is unchanged
    }

    [Fact]
    public async Task WorkspaceUpdateOrderingBlock_BuildBeforeEvaluation()
    {
        var u1 = BuildUpdate();
        var u2 = BuildUpdate();
        var u3 = BuildUpdate();
        var u4 = EvaluationUpdate();
        var u5 = BuildUpdate();
        var u6 = EvaluationUpdate();

        await VerifyOrderAsync(
            inputs:  new[] { u1, u2, u3, u4, u5, u6 },
            outputs: new[] { u4, u1, u2, u3, u5, u6 });
        //                   |<----------~~ evaluation moved forward
    }

    private static async Task VerifyOrderAsync(
        IReadOnlyList<IProjectVersionedValue<WorkspaceUpdate>> inputs,
        IReadOnlyList<IProjectVersionedValue<WorkspaceUpdate>> outputs)
    {
        var orderingBlock = WorkspaceFactory.CreateWorkspaceUpdateOrderingBlock();

        var broadcastBlock = new BroadcastBlock<IProjectVersionedValue<WorkspaceUpdate>>(null);

        broadcastBlock.LinkTo(orderingBlock, DataflowOption.PropagateCompletion);

        List<IProjectVersionedValue<WorkspaceUpdate>> actualOutputs = new();

        var actionBlock = new ActionBlock<IProjectVersionedValue<WorkspaceUpdate>>(actualOutputs.Add);

        orderingBlock.LinkTo(actionBlock, DataflowOption.PropagateCompletion);

        foreach (var input in inputs)
        {
            await broadcastBlock.SendAsync(input);
        }

        broadcastBlock.Complete();

        await actionBlock.Completion.WithTimeout(TimeSpan.FromSeconds(1));

        Assert.Equal(outputs.Count, actualOutputs.Count);

        for (int i = 0; i < outputs.Count; i++)
        {
            Assert.Same(outputs[i], actualOutputs[i]);
        }
    }

    private static IProjectVersionedValue<WorkspaceUpdate> EvaluationUpdate()
    {
        ConfiguredProject configuredProject = ConfiguredProjectFactory.Create();
        IProjectSubscriptionUpdate evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();
        IProjectSubscriptionUpdate sourceItemsUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();
        IProjectSnapshot projectSnapshot = IProjectSnapshot2Factory.Create();
        var workspaceUpdate = WorkspaceUpdate.FromEvaluation((configuredProject, projectSnapshot, evaluationRuleUpdate, sourceItemsUpdate));

        return new ProjectVersionedValue<WorkspaceUpdate>(
            workspaceUpdate,
            dataSourceVersions: ImmutableDictionary<NamedIdentity, IComparable>.Empty);
    }

    private static IProjectVersionedValue<WorkspaceUpdate> BuildUpdate()
    {
        ConfiguredProject configuredProject = ConfiguredProjectFactory.Create();
        IProjectSubscriptionUpdate buildRuleUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();
        var workspaceUpdate = WorkspaceUpdate.FromBuild((configuredProject, buildRuleUpdate));

        return new ProjectVersionedValue<WorkspaceUpdate>(
            workspaceUpdate,
            dataSourceVersions: ImmutableDictionary<NamedIdentity, IComparable>.Empty);
    }
}
