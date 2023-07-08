// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="RemoveLaunchProfile"/> actions.
    /// </summary>
    internal sealed class RemoveLaunchProfileAction : LaunchProfileActionBase
    {
        private readonly RemoveLaunchProfile _executableStep;

        public RemoveLaunchProfileAction(RemoveLaunchProfile executableStep)
        {
            _executableStep = executableStep;
        }

        protected override async Task ExecuteAsync(IQueryExecutionContext queryExecutionContext, IEntityValue projectEntity, IProjectLaunchProfileHandler launchProfileHandler, CancellationToken cancellationToken)
        {
            EntityIdentity? removedProfileId = await launchProfileHandler.RemoveLaunchProfileAsync(queryExecutionContext, projectEntity, _executableStep.ProfileName, cancellationToken);

            if (removedProfileId is not null)
            {
                RemovedLaunchProfiles.Add((projectEntity, removedProfileId));
            }
        }
    }
}
