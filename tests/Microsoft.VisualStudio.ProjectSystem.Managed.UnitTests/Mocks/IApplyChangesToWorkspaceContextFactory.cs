// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IApplyChangesToWorkspaceContextFactory
    {
        public static IApplyChangesToWorkspaceContext Create()
        {
            return Mock.Of<IApplyChangesToWorkspaceContext>();
        }

        public static IApplyChangesToWorkspaceContext ImplementApplyProjectBuild(Action<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot)>, ContextState, CancellationToken> action)
        {
            var mock = new Mock<IApplyChangesToWorkspaceContext>();
            mock.Setup(c => c.ApplyProjectBuild(It.IsAny<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot)>>(), It.IsAny<ContextState>(), It.IsAny<CancellationToken>()))
                .Callback(action);

            return mock.Object;
        }

        public static IApplyChangesToWorkspaceContext ImplementApplyProjectEvaluation(Action<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>, ContextState, CancellationToken> action)
        {
            var mock = new Mock<IApplyChangesToWorkspaceContext>();
            mock.Setup(c => c.ApplyProjectEvaluation(It.IsAny<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>>(), It.IsAny<ContextState>(), It.IsAny<CancellationToken>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
