// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    /// Creates an in-memory launch profile representing launching the web server from data in the flavored project section
    /// </summary>
    internal partial class LaunchProfileInitializer 
    {
        internal class LaunchProfileInitializerInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly ILaunchSettingsProvider2 _launchSettingsProvider;
            private readonly IUnconfiguredProjectCommonServices _projectServices;
            private readonly WebLaunchSettingsProvider _webLaunchSettingsProvider;
            private readonly WebServer _webServer;
            private readonly IProjectTreeService _projectTree;

            public LaunchProfileInitializerInstance(
                ILaunchSettingsProvider2 launchSettingsProvider,
                IUnconfiguredProjectCommonServices projectServices,
                WebLaunchSettingsProvider webLaunchSettingsProvider,
                WebServer webServer,
                IProjectTreeService projectTree)
              : 
                 base(projectServices.ThreadingService.JoinableTaskContext)
            {
                _launchSettingsProvider = launchSettingsProvider;
                _projectServices = projectServices;
                _webLaunchSettingsProvider = webLaunchSettingsProvider;
                _webServer = webServer;
                _projectTree = projectTree;
            }

            public Task InitializeAsync()
            {
                _projectServices.ThreadingService.RunAndForget(() =>
                {
                    return InitializeAsync(DisposalToken);
                }, _projectServices.Project, ProjectFaultSeverity.Recoverable);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                return Task.CompletedTask;
            }

            protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
            {

                // waiting for the first non-null tree ensures this does not run too early
                await _projectTree.PublishAnyNonNullTreeAsync(DisposalToken);

                // Get the currrent settings and add a new one to represent launching aspnet
                var currentSettings = await _launchSettingsProvider.SourceBlock.ReceiveAsync(DisposalToken);

                // Since there shouldn't be a launchsettings file, it should have added a "Project" profile
                if (currentSettings.Profiles.Count == 1)
                {
                    // Change the name of the project one to the type of web server being launched

                    var existingProfile = currentSettings.Profiles.First();
                    var newProfile = new LaunchProfile(existingProfile);
                    newProfile.Name = "IIS Express";
                    newProfile.DoNotPersist = true;

                    await _launchSettingsProvider.AddOrUpdateProfileAsync(newProfile, addToFront: true);
                    if (existingProfile.Name != null)
                    {
                        await _launchSettingsProvider.RemoveProfileAsync(existingProfile.Name);
                    }

                    // Now set the active profile to the one we just set.
                    await  _launchSettingsProvider.SetActiveProfileAsync("IIS Express");
                
                    // Configure the webserver 
                    try
                    {
                        await _webServer.ConfigureWebServerAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Assert(false, ex.Message);
                    }
                }
            }
        }
    }
}
