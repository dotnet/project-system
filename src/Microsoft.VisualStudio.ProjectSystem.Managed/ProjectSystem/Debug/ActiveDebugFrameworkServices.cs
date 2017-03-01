// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Providers a wrapper around the what is considered the active debugging framework.
    /// </summary>
    [Export(typeof(IActiveDebugFrameworkServices))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ActiveDebugFrameworkServices : IActiveDebugFrameworkServices
    {
        const int MaxFrameworks = 20;

        [ImportingConstructor]
        public ActiveDebugFrameworkServices(IActiveConfiguredProjectsProvider configuredProjectsProvider, IUnconfiguredProjectCommonServices commonProjectServices)
        {
            ActiveConfiguredProjectsProvider = configuredProjectsProvider;
            CommonProjectServices = commonProjectServices;
        }

        IActiveConfiguredProjectsProvider ActiveConfiguredProjectsProvider { get; }
        IUnconfiguredProjectCommonServices CommonProjectServices { get; }

        /// <summary>
        /// <see cref="IActiveDebugFrameworkServices.GetProjectFrameworksAsync"/>
        /// </summary>
        public async Task<List<string>> GetProjectFrameworksAsync()
        {
            // It is important that we return the frameworks in the order they are specified in the project to ensure the default is set
            // correctly. 
            var props = await CommonProjectServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);

            var targetFrameworks = (string)await props.TargetFrameworks.GetValueAsync().ConfigureAwait(false);

            if(!string.IsNullOrWhiteSpace(targetFrameworks))
            {
                return TargetFrameworkProjectConfigurationDimensionProvider.ParseTargetFrameworks(targetFrameworks).ToList();
            }
            return null;
        }

        /// <summary>
        /// <see cref="IActiveDebugFrameworkServices.SetActiveDebuggingFrameworkPropertyAsync"/>
        /// </summary>
        public async Task SetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
        {
            var props = await CommonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync().ConfigureAwait(false);
            await props.ActiveDebugFramework.SetValueAsync(activeFramework).ConfigureAwait(true);
        }

        /// <summary>
        /// <see cref="IActiveDebugFrameworkServices.GetActiveDebuggingFrameworkPropertyAsync"/>
        /// </summary>
        public async Task<string> GetActiveDebuggingFrameworkPropertyAsync()
        {
            var props = await CommonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync().ConfigureAwait(false);
            var activeValue = await props.ActiveDebugFramework.GetValueAsync().ConfigureAwait(true) as string;
            return activeValue;
        }

        /// <summary>
        /// <see cref="IActiveDebugFrameworkServices.GetConfiguredProjectForActiveFrameworkAsync"/>
        /// </summary>
        public async Task<ConfiguredProject> GetConfiguredProjectForActiveFrameworkAsync()
        {
            var configProjects = await ActiveConfiguredProjectsProvider.GetActiveConfiguredProjectsMapAsync().ConfigureAwait(false);

            // If there is only one we are done
            if(configProjects.Count == 1)
            {
                return configProjects.First().Value;
            }
            
            var activeFramework = await GetActiveDebuggingFrameworkPropertyAsync().ConfigureAwait(false);
            if(!string.IsNullOrWhiteSpace(activeFramework))
            {
                if(configProjects.TryGetValue(activeFramework, out ConfiguredProject configuredProject))
                {
                    return configuredProject;
                }
            }

            // We can't just select the first one. If activeFramework is not set we must pick the first one as defined by the 
            // targetFrameworks property. So we need the order as returned by GetProjectFrameworks()
            var frameworks = await GetProjectFrameworksAsync().ConfigureAwait(false);
            if(frameworks != null &&  frameworks.Count > 0)
            {
                if(configProjects.TryGetValue(frameworks[0], out ConfiguredProject configuredProject))
                {
                    return configuredProject;
                }
            }

            // All that is left is to return the first one.
            return configProjects.First().Value;
        }
   }
}
