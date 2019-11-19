// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using ExportOrder = Microsoft.VisualStudio.ProjectSystem.OrderAttribute;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Handles the NoAction profile so that Ctrl-f5\F5 throws an error to the user
    /// </summary>
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [ExportOrder(1000)] // High number so it called first
    internal class ErrorProfileDebugTargetsProvider : IDebugProfileLaunchTargetsProvider
    {
        [ImportingConstructor]
        public ErrorProfileDebugTargetsProvider(ConfiguredProject configuredProject)
        {
            _configuredProject = configuredProject;
        }

        private readonly ConfiguredProject _configuredProject;

        /// <summary>
        /// This provider handles the NoAction profile
        /// </summary>
        public bool SupportsProfile(ILaunchProfile profile)
        {
            return string.Equals(profile.CommandName, LaunchSettingsProvider.ErrorProfileCommandName);
        }

        /// <summary>
        /// Called just prior to launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            // Nothing to do here
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called just prior to launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            // Nothing to do here
            return Task.CompletedTask;
        }

        /// <summary>
        /// When F5\Ctrl-F5 is invoked on a NoAction profile and error is thrown to the user. Typical case is trying to run a 
        /// class library project
        /// </summary>
        public Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
        {
            if (activeProfile.OtherSettings.TryGetValue("ErrorString", out object objErrorString) && objErrorString is string errorString)
            {
                throw new Exception(string.Format(VSResources.ErrorInProfilesFile2, Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath), errorString));
            }

            throw new Exception(string.Format(VSResources.ErrorInProfilesFile, Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath)));
        }
    }
}
