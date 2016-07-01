// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    /// <summary>
    /// Base class for Project Property Providers that delegate most of their
    /// responsibilities to a different project property provider, only overriding
    /// the things they want to change.
    /// </summary>
    internal class DelegatedProjectPropertiesProviderBase : IProjectPropertiesProvider
    {
        /// <summary>
        /// Gets the unconfigured project
        /// </summary>
        [Import]
        protected internal UnconfiguredProject UnconfiguredProject { get; set; }

        /// <summary>
        /// The Project Properties Provider that is delegated to for most operations
        /// </summary>
        protected internal readonly IProjectPropertiesProvider DelegatedProvider;

        /// <summary>
        /// Construct using the provider that should be delegated to for most operations
        /// </summary>
        public DelegatedProjectPropertiesProviderBase(IProjectPropertiesProvider provider)
        {
            Requires.NotNull(provider, nameof(provider));

            DelegatedProvider = provider;
        }

        public virtual string DefaultProjectPath => UnconfiguredProject.FullPath;

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanged
        {
            add { DelegatedProvider.ProjectPropertyChanged += value; }
            remove { DelegatedProvider.ProjectPropertyChanged += value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChangedOnWriter
        {
            add { DelegatedProvider.ProjectPropertyChangedOnWriter += value; }
            remove { DelegatedProvider.ProjectPropertyChangedOnWriter += value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanging
        {
            add { DelegatedProvider.ProjectPropertyChanging += value; }
            remove { DelegatedProvider.ProjectPropertyChanging += value; }
        }

        public virtual IProjectProperties GetCommonProperties()
            => DelegatedProvider.GetCommonProperties();

        public virtual IProjectProperties GetItemProperties(string itemType, string item)
            => DelegatedProvider.GetItemProperties(itemType, item);

        public virtual IProjectProperties GetItemTypeProperties(string itemType)
            => DelegatedProvider.GetItemTypeProperties(itemType);

        public virtual IProjectProperties GetProperties(string file, string itemType, string item)
            => DelegatedProvider.GetProperties(file, itemType, item);
    }
}
