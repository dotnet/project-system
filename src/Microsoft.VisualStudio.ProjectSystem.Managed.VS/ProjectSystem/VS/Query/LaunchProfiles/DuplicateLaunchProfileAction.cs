// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModelMethods.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="ProjectModelActionNames.DuplicateLaunchProfile"/> actions.
    /// </summary>
    internal sealed class DuplicateLaunchProfileAction : LaunchProfileActionBase
    {
        private readonly DuplicateLaunchProfile _executableStep;

        public DuplicateLaunchProfileAction(DuplicateLaunchProfile executableStep)
        {
            _executableStep = executableStep;
        }

        protected override async Task ExecuteAsync(ILaunchSettingsProvider launchSettingsProvider, CancellationToken cancellationToken)
        {
            ILaunchSettings? launchSettings = await launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite).WithCancellation(cancellationToken);
            Assumes.NotNull(launchSettings);

            ILaunchProfile? existingProfile = launchSettings.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, _executableStep.CurrentProfileName));
            if (existingProfile is not null)
            {
                var writableProfile = new WritableLaunchProfile(existingProfile);
                writableProfile.Name = _executableStep.NewProfileName;
                writableProfile.CommandName = _executableStep.NewProfileCommandName;

                await launchSettingsProvider.AddOrUpdateProfileAsync(writableProfile.ToLaunchProfile(), addToFront: false);
            }
        }
    }
}
