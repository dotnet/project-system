// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

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
                public ConfiguredProject ActiveConfiguredProject { get; }
                public IProjectVersionedValue<IProjectSubscriptionUpdate> Subscription { get; }
                public CommandLineArgumentsSnapshot? CommandLineArgumentsSnapshot { get; }
                public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }

                public ProjectChange(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription, CommandLineArgumentsSnapshot commandLineArguments)> update)
                {
                    ActiveConfiguredProject = update.Value.project;
                    Subscription = update.Derive(u => u.subscription);
                    CommandLineArgumentsSnapshot = update.Value.commandLineArguments;
                    DataSourceVersions = update.DataSourceVersions;
                }

                public ProjectChange(IProjectVersionedValue<(ConfiguredProject project, IProjectSubscriptionUpdate subscription)> update)
                {
                    ActiveConfiguredProject = update.Value.project;
                    Subscription = update.Derive(u => u.subscription);
                    DataSourceVersions = update.DataSourceVersions;
                }
            }
        }
    }
}
