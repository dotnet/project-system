// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// C#/VB specific project reload interceptor.
    /// </summary>
    [Export(typeof(IProjectReloadInterceptor))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal sealed class ProjectReloadInterceptor : IProjectReloadInterceptor
    {
        [ImportingConstructor]
        public ProjectReloadInterceptor()
        {
        }

        public ProjectReloadResult InterceptProjectReload(ImmutableArray<ProjectPropertyElement> oldProperties, ImmutableArray<ProjectPropertyElement> newProperties)
        {
            if (NeedsForcedReload(oldProperties, newProperties))
            {
                return ProjectReloadResult.NeedsForceReload;
            }

            return ProjectReloadResult.NoAction;
        }

        private static bool NeedsForcedReload(ImmutableArray<ProjectPropertyElement> oldProperties, ImmutableArray<ProjectPropertyElement> newProperties)
        {
            // If user added or removed TargetFramework/TargetFrameworks property, then force a full project reload.
            (bool hasTargetFramework, bool hasTargetFrameworks) = ComputeProjectTargets(oldProperties);
            var newTargets = ComputeProjectTargets(newProperties);

            return hasTargetFramework != newTargets.hasTargetFramework || hasTargetFrameworks != newTargets.hasTargetFrameworks;
        }

        private static (bool hasTargetFramework, bool hasTargetFrameworks) ComputeProjectTargets(ImmutableArray<ProjectPropertyElement> properties)
        {
            (bool hasTargetFramework, bool hasTargetFrameworks) targets = (false, false);

            foreach (var property in properties)
            {
                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworkProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.hasTargetFramework = true;
                }

                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworksProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.hasTargetFrameworks = true;
                }
            }

            return targets;
        }
    }
}