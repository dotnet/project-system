// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    public sealed class UpToDateCheckImplicitConfiguredInputTests : BuildUpToDateCheckTestBase
    {
        [Fact]
        public void Update_InitialItemDataDoesNotUpdateLastItemsChangedAtUtc()
        {
            // This test covers a false negative described in https://github.com/dotnet/project-system/issues/5386
            // where the initial snapshot of items sets LastItemsChangedAtUtc, so if a project is up to date when
            // it is loaded, then the items are considered changed *after* the last build, but MSBuild's up-to-date
            // check will determine the project doesn't require a rebuild and so the output timestamps won't update.
            // This previously left the project in a state where it would be considered out of date endlessly.

            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>()
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot1 = new Dictionary<string, IProjectRuleSnapshotModel>()
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var sourceSnapshot2 = new Dictionary<string, IProjectRuleSnapshotModel>()
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1", "ItemPath2")
            };

            var state = UpToDateCheckImplicitConfiguredInput.CreateEmpty(ProjectConfigurationFactory.Create("testConfiguration"));

            Assert.Null(state.LastItemsChangedAtUtc);

            // Initial change does NOT set LastItemsChangedAtUtc
            state = UpdateState(
                state,
                projectSnapshot,
                sourceSnapshot1);

            Assert.Null(state.LastItemsChangedAtUtc);

            // Broadcasting an update with no change to items does NOT set LastItemsChangedAtUtc
            state = UpdateState(state);

            Assert.Null(state.LastItemsChangedAtUtc);

            // Broadcasting changed items DOES set LastItemsChangedAtUtc
            state = UpdateState(
                state,
                projectSnapshot,
                sourceSnapshot2);

            Assert.NotNull(state.LastItemsChangedAtUtc);
        }
    }
}
