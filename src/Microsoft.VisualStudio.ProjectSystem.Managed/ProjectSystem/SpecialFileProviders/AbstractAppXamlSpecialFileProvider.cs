// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            if (node is null)
            {
                // Otherwise, find a candidate that we might be able to add to the project
                node = await base.FindFileAsync(provider, root);
            }

            return node;
        }

        private static IProjectTree? FindAppXamlFile(IProjectTree root)
        {
            foreach (IProjectItemTree item in root.GetSelfAndDescendentsBreadthFirst().OfType<IProjectItemTree>())
            {
                if (StringComparers.ItemTypes.Equals(item.Item?.ItemType, "ApplicationDefinition"))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
