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
        public TreeViewProviderBase(UnconfiguredProject unconfiguredProject)
        {
            ProjectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                            ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                            projectCapabilityCheckProvider: unconfiguredProject);
        }

        /// <summary>
        /// Gets the collection of <see cref="IProjectTreePropertiesProvider"/> imports 
        /// that apply to the references tree.
        /// </summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> ProjectTreePropertiesProviders { get; set; }


        public abstract Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree, 
            IDependenciesSnapshot snapshot, 
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract IProjectTree FindByPath(IProjectTree root, string path);

        protected ProjectTreeCustomizablePropertyContext GetCustomPropertyContext(IProjectTree parent)
        {
            return new ProjectTreeCustomizablePropertyContext
            {
                ExistsOnDisk = false,
                ParentNodeFlags = parent?.Flags ?? default(ProjectTreeFlags)
            };
        }

        protected void ApplyProjectTreePropertiesCustomization(
                        IProjectTreeCustomizablePropertyContext context,
                        ReferencesProjectTreeCustomizablePropertyValues values)
        {
            foreach (var provider in ProjectTreePropertiesProviders.ExtensionValues())
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

            public bool IsFolder
            {
                get
                {
                    return false;
                }
            }

            public bool IsNonFileSystemProjectItem
            {
                get
                {
                    return true;
                }
            }

            public IImmutableDictionary<string, string> ProjectTreeSettings { get; set; }
        }
    }
}
