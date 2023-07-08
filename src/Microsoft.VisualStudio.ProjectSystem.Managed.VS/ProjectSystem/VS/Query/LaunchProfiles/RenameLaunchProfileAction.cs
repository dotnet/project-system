// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="RenameLaunchProfile"/> actions.
    /// </summary>
    internal sealed class RenameLaunchProfileAction : LaunchProfileActionBase
    {
        private readonly RenameLaunchProfile _executableStep;

        public RenameLaunchProfileAction(RenameLaunchProfile executableStep)
        {
            _executableStep = executableStep;
        }

        protected override async Task ExecuteAsync(IQueryExecutionContext queryExecutionContext, IEntityValue projectEntity, IProjectLaunchProfileHandler launchProfileHandler, CancellationToken cancellationToken)
        {
            (EntityIdentity currentProfileId, EntityIdentity newProfileId)? changes = await launchProfileHandler.RenameLaunchProfileAsync(queryExecutionContext, projectEntity, _executableStep.CurrentProfileName, _executableStep.NewProfileName, cancellationToken);

            if (changes.HasValue)
            {
                RemovedLaunchProfiles.Add((projectEntity, changes.Value.currentProfileId));
                AddedLaunchProfiles.Add((projectEntity, changes.Value.newProfileId));
            }
        }
    }
}
