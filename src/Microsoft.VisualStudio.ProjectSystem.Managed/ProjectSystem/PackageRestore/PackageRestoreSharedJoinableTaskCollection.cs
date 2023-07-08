// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    [Export(typeof(PackageRestoreSharedJoinableTaskCollection))]
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal class PackageRestoreSharedJoinableTaskCollection : IJoinableTaskScope
    {
        [ImportingConstructor]
        public PackageRestoreSharedJoinableTaskCollection(IProjectThreadingService threadingService)
        {
            JoinableTaskCollection = threadingService.JoinableTaskContext.CreateCollection();
            JoinableTaskCollection.DisplayName = nameof(PackageRestoreSharedJoinableTaskCollection);
            JoinableTaskFactory = threadingService.JoinableTaskContext.CreateFactory(JoinableTaskCollection);
        }

        public JoinableTaskCollection JoinableTaskCollection { get; }

        public JoinableTaskFactory JoinableTaskFactory { get; }
    }
}
