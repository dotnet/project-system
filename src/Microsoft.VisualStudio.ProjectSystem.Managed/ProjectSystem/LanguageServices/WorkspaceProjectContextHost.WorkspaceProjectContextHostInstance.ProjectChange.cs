// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextHost
    {
        internal partial class WorkspaceProjectContextHostInstance
        {
            /// <summary>
            /// Converts a dataflow update into a data structure that is easier to deal with
            /// </summary>
            internal class ProjectChange
            {
                public ConfiguredProject Project { get; }
                public IProjectVersionedValue<IProjectSubscriptionUpdate> Subscription { get; }
                public IProjectBuildSnapshot? BuildSnapshot { get; }
                public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }

                public ProjectChange(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription, IProjectBuildSnapshot buildSnapshot)> update)
                {
                    Project = update.Value.project;
                    Subscription = update.Derive(u => u.subscription);
                    BuildSnapshot = update.Value.buildSnapshot;
                    DataSourceVersions = update.DataSourceVersions;
                }

                public ProjectChange(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription)> update)
                {
                    Project = update.Value.project;
                    Subscription = update.Derive(u => u.subscription);
                    DataSourceVersions = update.DataSourceVersions;
                }
            }
        }
    }
}
