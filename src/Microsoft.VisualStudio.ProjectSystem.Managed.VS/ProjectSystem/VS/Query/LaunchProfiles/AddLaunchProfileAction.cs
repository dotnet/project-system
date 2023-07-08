// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="AddLaunchProfile"/> actions.
    /// </summary>
    internal sealed class AddLaunchProfileAction : LaunchProfileActionBase
    {
        private readonly AddLaunchProfile _executableStep;

        public AddLaunchProfileAction(AddLaunchProfile executableStep)
        {
            _executableStep = executableStep;
        }

        protected override async Task ExecuteAsync(IQueryExecutionContext queryExecutionContext, IEntityValue projectEntity, IProjectLaunchProfileHandler handler, CancellationToken cancellationToken)
        {
            EntityIdentity? newLaunchProfileId = await handler.AddLaunchProfileAsync(queryExecutionContext, projectEntity, _executableStep.CommandName, _executableStep.NewProfileName, cancellationToken);

            if (newLaunchProfileId is not null)
            {
                AddedLaunchProfiles.Add((projectEntity, newLaunchProfileId));
            }
        }
    }
}
