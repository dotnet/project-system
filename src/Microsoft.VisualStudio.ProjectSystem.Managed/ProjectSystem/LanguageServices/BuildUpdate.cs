// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Groups data related to build updates into a single object.
/// </summary>
internal sealed class BuildUpdate
{
    public BuildUpdate(IProjectSubscriptionUpdate buildRuleUpdate, CommandLineArgumentsSnapshot commandLineArgumentsSnapshot)
    {
        Requires.NotNull(buildRuleUpdate, nameof(buildRuleUpdate));
        Requires.NotNull(commandLineArgumentsSnapshot, nameof(commandLineArgumentsSnapshot));

        BuildRuleUpdate = buildRuleUpdate;
        CommandLineArgumentsSnapshot = commandLineArgumentsSnapshot;
    }

    public IProjectSubscriptionUpdate BuildRuleUpdate { get; }

    public CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot { get; }
}
