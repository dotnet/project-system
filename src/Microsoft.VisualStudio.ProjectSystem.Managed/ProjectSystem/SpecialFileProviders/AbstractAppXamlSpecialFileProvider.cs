// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides the <see langword="abstract"/> base class for <see cref="ISpecialFileProvider"/> instances 
    ///     that handle the WPF Application Definition file.
    /// </summary>
    internal abstract class AbstractAppXamlSpecialFileProvider : AbstractFindByNameSpecialFileProvider
    {
        protected AbstractAppXamlSpecialFileProvider(string fileName, IPhysicalProjectTree projectTree)
            : base(fileName, projectTree)
        {
        }

        protected override async Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            // First look for the actual App.xaml first
            IProjectTree? node = FindAppXamlFile(root);
            if (node == null)
            {
                // Otherwise, find a candidate that we might be able to add to the project
                node = await base.FindFileAsync(provider, root);
            }

            return node;
        }

        private IProjectTree? FindAppXamlFile(IProjectTree root)
        {
            foreach (IProjectItemTree item in root.GetSelfAndDescendentsBreadthFirst().OfType<IProjectItemTree>())
            {
                if (StringComparers.ItemTypes.Equals(item.Item.ItemType, ApplicationDefinition.SchemaName))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
