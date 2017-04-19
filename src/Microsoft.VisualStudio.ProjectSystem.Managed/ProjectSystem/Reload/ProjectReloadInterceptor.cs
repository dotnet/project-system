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
            // If user added/removed/edited TargetFramework/TargetFrameworks property, then force a full project reload.
            var oldTargets = ComputeProjectTargets(oldProperties);
            var newTargets = ComputeProjectTargets(newProperties);

            return !StringComparers.PropertyValues.Equals(oldTargets.TargetFramework, newTargets.TargetFramework) ||
                !StringComparers.PropertyValues.Equals(oldTargets.TargetFrameworks, newTargets.TargetFrameworks);
        }

        private static (string TargetFramework, string TargetFrameworks) ComputeProjectTargets(ImmutableArray<ProjectPropertyElement> properties)
        {
            (string TargetFramework, string TargetFrameworks) targets = (null, null);

            foreach (var property in properties)
            {
                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworkProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.TargetFramework = property.Value;
                }

                if (property.Name.Equals(ConfigurationGeneral.TargetFrameworksProperty, StringComparison.OrdinalIgnoreCase))
                {
                    targets.TargetFrameworks = property.Value;
                }
            }

            return targets;
        }
    }
}