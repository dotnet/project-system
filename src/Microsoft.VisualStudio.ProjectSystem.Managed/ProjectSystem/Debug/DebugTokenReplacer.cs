// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Token replacer can be imported to replace all the msbuild property and environment variable tokens in an ILaunchProfile or
    /// in an individual string
    /// </summary>
    [Export(typeof(IDebugTokenReplacer))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class DebugTokenReplacer : IDebugTokenReplacer
    {

        [ImportingConstructor]
        public DebugTokenReplacer(IUnconfiguredProjectCommonServices unconnfiguredServices, IEnvironmentHelper environmentHelper)
        {
            UnconfiguredServices = unconnfiguredServices;
            EnvironmentHelper = environmentHelper;
        }

        private IUnconfiguredProjectCommonServices UnconfiguredServices { get; }
        private IEnvironmentHelper EnvironmentHelper { get; }

        // Regular expression string to extract $(sometoken) elements from a string
        private static Regex MatchTokenRegex = new Regex(@"(\$\((?<token>[^\)]+)\))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Walks the profile and returns a new one where all the tokens have been replaced. Tokens can consist of 
        /// environment variables (%var%), or any msbuild property $(msbuildproperty). Environment variables are 
        /// replaced first, followed by msbuild properties.
        /// </summary>
        public async Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile)
        {
            LaunchProfile resolvedProfile = new LaunchProfile(profile);
            if(!string.IsNullOrWhiteSpace(resolvedProfile.ExecutablePath))
            {
                resolvedProfile.ExecutablePath = await ReplaceTokensInStringAsync(resolvedProfile.ExecutablePath, true).ConfigureAwait(false);
            }
            
            if(!string.IsNullOrWhiteSpace(resolvedProfile.CommandLineArgs))
            {
                resolvedProfile.CommandLineArgs = await ReplaceTokensInStringAsync(resolvedProfile.CommandLineArgs, true).ConfigureAwait(false);
            }
            
            if(!string.IsNullOrWhiteSpace(resolvedProfile.WorkingDirectory))
            {
                resolvedProfile.WorkingDirectory = await ReplaceTokensInStringAsync(resolvedProfile.WorkingDirectory, true).ConfigureAwait(false);
            }
        
            if(!string.IsNullOrWhiteSpace(resolvedProfile.LaunchUrl))
            {
                resolvedProfile.LaunchUrl = await ReplaceTokensInStringAsync(resolvedProfile.LaunchUrl, true).ConfigureAwait(false);
            }

            // Since Env variables are an immutable dictionary they are a little messy to update.
            if(resolvedProfile.EnvironmentVariables != null)
            {
                foreach(var kvp in resolvedProfile.EnvironmentVariables)
                {
                    resolvedProfile.EnvironmentVariables = resolvedProfile.EnvironmentVariables.SetItem(kvp.Key,  await ReplaceTokensInStringAsync(kvp.Value, true).ConfigureAwait(false));
                }
            }

            if(resolvedProfile.OtherSettings != null)
            {
                foreach(var kvp in resolvedProfile.OtherSettings)
                {
                    if(kvp.Value is string)
                    {
                        resolvedProfile.OtherSettings = resolvedProfile.OtherSettings.SetItem(kvp.Key,  await ReplaceTokensInStringAsync((string)kvp.Value, true).ConfigureAwait(false));
                    }

                }
            }

            return resolvedProfile;
        }

        /// <summary>
        /// Replaces the tokens and environment variables in a single string. If expandEnvironmentVars
        /// is true, they are expanded first before replacement happens. If the rawString is null or empty
        /// it is returned as is.
        /// </summary>
        public async Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars)
        {
            if (string.IsNullOrWhiteSpace(rawString))
            {
                return rawString;
            }

            string updatedString = expandEnvironmentVars? EnvironmentHelper.ExpandEnvironmentVariables(rawString) : rawString;

            var matches = MatchTokenRegex.Matches(updatedString);
            if(matches.Count > 0)
            {
                using (var access = AccessProject())
                {
                    var project = await access.GetProjectAsync().ConfigureAwait(true);

                    // For each token we try to get a replacement value.
                    foreach(Match match in matches)
                    {
                        // Resovlve with msbuild. If will return the empty string if not found
                        updatedString = updatedString.Replace(match.Value, project.ExpandString(match.Value));
                    }
                }
            }
            return updatedString;
        }

        /// <summary>
        /// This is here to support unit tests which can derive from this class and return their own 
        /// instance of IProjectReadAccess
        /// </summary>
        protected virtual IProjectReadAccess AccessProject()
        {
            return new ProjectReadAccessor(UnconfiguredServices.ProjectLockService, UnconfiguredServices.ActiveConfiguredProject);
        }
    }

    /// <summary>
    /// Mocking the Project Lock system is very difficult due to internal constructors in CPS. This interface
    /// and class abstracts getting the lock and accessing the project
    /// </summary>
    internal interface IProjectReadAccess : IDisposable
    {
        Task<Microsoft.Build.Evaluation.Project>  GetProjectAsync();
    }

    internal class ProjectReadAccessor : IProjectReadAccess
    {
        IProjectLockService ProjectLockService { get; set; }
        ConfiguredProject ConfiguredProject { get; set; }
        ProjectLockReleaser? Access { get; set; }

        public ProjectReadAccessor(IProjectLockService lockService, ConfiguredProject configuredProject)
        {
            ProjectLockService = lockService;
            ConfiguredProject = configuredProject;
        }

        public async Task<Microsoft.Build.Evaluation.Project> GetProjectAsync()
        {
            // If we already have access we can just use it to get an instance of the project
            if(Access == null)
            {
                Access = await this.ProjectLockService.ReadLockAsync();

            }

            return await Access.Value.GetProjectAsync(ConfiguredProject).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if(Access != null)
            {
                Access.Value.Dispose();
                Access = null;
            }
        }
    }
}
