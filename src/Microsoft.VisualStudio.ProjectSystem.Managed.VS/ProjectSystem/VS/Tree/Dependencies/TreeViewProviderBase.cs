// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal abstract class TreeViewProviderBase : IDependenciesTreeViewProvider
    {
        protected TreeViewProviderBase(UnconfiguredProject project)
        {
            _projectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: project);
        }

        /// <summary>
        /// Gets the collection of <see cref="IProjectTreePropertiesProvider"/> imports 
        /// that apply to the references tree.
        /// </summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private readonly OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> _projectTreePropertiesProviders;

        public abstract Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree,
            IDependenciesSnapshot snapshot,
            CancellationToken cancellationToken = default);

        public abstract IProjectTree FindByPath(IProjectTree root, string path);

        protected ProjectTreeCustomizablePropertyContext GetCustomPropertyContext(IProjectTree parent)
        {
            return new ProjectTreeCustomizablePropertyContext
            {
                ExistsOnDisk = false,
                ParentNodeFlags = parent?.Flags ?? default
            };
        }

        protected void ApplyProjectTreePropertiesCustomization(
            IProjectTreeCustomizablePropertyContext context,
            ReferencesProjectTreeCustomizablePropertyValues values)
        {
            foreach (IProjectTreePropertiesProvider provider in _projectTreePropertiesProviders.ExtensionValues())
            {
                provider.CalculatePropertyValues(context, values);
            }
        }

        /// <summary>
        /// A private implementation of <see cref="IProjectTreeCustomizablePropertyContext"/>.
        /// </summary>
        protected class ProjectTreeCustomizablePropertyContext : IProjectTreeCustomizablePropertyContext
        {
            public string ItemName { get; set; }

            public string ItemType { get; set; }

            public IImmutableDictionary<string, string> Metadata { get; set; }

            public ProjectTreeFlags ParentNodeFlags { get; set; }

            public bool ExistsOnDisk { get; set; }

            public bool IsFolder => false;

            public bool IsNonFileSystemProjectItem => true;

            public IImmutableDictionary<string, string> ProjectTreeSettings { get; set; }
        }
    }
}
