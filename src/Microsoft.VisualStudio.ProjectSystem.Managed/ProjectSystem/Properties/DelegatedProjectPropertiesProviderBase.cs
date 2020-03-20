// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Base class for Project Property Providers that delegate most of their
    /// responsibilities to a different project property provider, only overriding
    /// the things they want to change.
    /// </summary>
    internal class DelegatedProjectPropertiesProviderBase : IProjectPropertiesProvider, IProjectInstancePropertiesProvider
    {
        /// <summary>
        /// Gets the unconfigured project
        /// </summary>
        protected readonly UnconfiguredProject Project;

        /// <summary>
        /// The Project Properties Provider that is delegated to for most operations
        /// </summary>
        protected readonly IProjectPropertiesProvider DelegatedProvider;

        /// <summary>
        /// The Project Instance Properties provider that is being delegated to for most operations
        /// </summary>
        protected readonly IProjectInstancePropertiesProvider DelegatedInstanceProvider;

        /// <summary>
        /// Construct using the provider that should be delegated to for most operations
        /// </summary>
        public DelegatedProjectPropertiesProviderBase(IProjectPropertiesProvider provider, IProjectInstancePropertiesProvider instanceProvider, UnconfiguredProject project)
        {
            Requires.NotNull(provider, nameof(provider));
            Requires.NotNull(instanceProvider, nameof(instanceProvider));
            Requires.NotNull(project, nameof(project));

            DelegatedProvider = provider;
            DelegatedInstanceProvider = instanceProvider;
            Project = project;
        }

        public virtual string DefaultProjectPath
        {
            get
            {
                Assumes.NotNull(Project.FullPath);
                return Project.FullPath;
            }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanged
        {
            add { DelegatedProvider.ProjectPropertyChanged += value; }
            remove { DelegatedProvider.ProjectPropertyChanged -= value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChangedOnWriter
        {
            add { DelegatedProvider.ProjectPropertyChangedOnWriter += value; }
            remove { DelegatedProvider.ProjectPropertyChangedOnWriter -= value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanging
        {
            add { DelegatedProvider.ProjectPropertyChanging += value; }
            remove { DelegatedProvider.ProjectPropertyChanging -= value; }
        }

        public virtual IProjectProperties GetCommonProperties()
            => DelegatedProvider.GetCommonProperties();

        public virtual IProjectProperties GetItemProperties(string? itemType, string? item)
            => DelegatedProvider.GetItemProperties(itemType, item);

        public virtual IProjectProperties GetItemTypeProperties(string? itemType)
            => DelegatedProvider.GetItemTypeProperties(itemType);

        public virtual IProjectProperties GetProperties(string file, string? itemType, string? item)
            => DelegatedProvider.GetProperties(file, itemType, item);

        public virtual IProjectProperties GetCommonProperties(ProjectInstance projectInstance)
            => DelegatedInstanceProvider.GetCommonProperties(projectInstance);

        public virtual IProjectProperties GetItemTypeProperties(ProjectInstance projectInstance, string? itemType)
            => DelegatedInstanceProvider.GetItemTypeProperties(projectInstance, itemType);

        public virtual IProjectProperties GetItemProperties(ProjectInstance projectInstance, string? itemType, string? itemName)
            => DelegatedInstanceProvider.GetItemProperties(projectInstance, itemType, itemName);

        public virtual IProjectProperties GetItemProperties(ITaskItem taskItem)
            => DelegatedInstanceProvider.GetItemProperties(taskItem);
    }
}
