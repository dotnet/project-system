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
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
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
            var oldTargets = ComputeProjectTargets(oldProperties);
            var newTargets = ComputeProjectTargets(newProperties);

            return oldTargets.HasTargetFramework != newTargets.HasTargetFramework || oldTargets.HasTargetFrameworks != newTargets.HasTargetFrameworks;
        }

        private static (bool HasTargetFramework, bool HasTargetFrameworks) ComputeProjectTargets(ImmutableArray<ProjectPropertyElement> properties)
        {
            (bool HasTargetFramework, bool HasTargetFrameworks) targets = (false, false);

            foreach (var property in properties)
            {
                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworkProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.HasTargetFramework = true;
                }

                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworksProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.HasTargetFrameworks = true;
                }
            }

            return targets;
        }
    }
}