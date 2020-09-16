// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Simplifies some operations that are common across the property page query data providers.
    /// </summary>
    internal static class PropertyPageQueryExtensions
    {
        /// <summary>
        /// Retrieves the project-level <see cref="IPropertyPagesCatalog"/> for an <see cref="UnconfiguredProject"/>.
        /// </summary>
        public static async Task<IPropertyPagesCatalog?> GetProjectLevelPropertyPagesCatalogAsync(this UnconfiguredProject project)
        {
            if (await project.GetSuggestedConfiguredProjectAsync() is ConfiguredProject configuredProject)
            {
                return await configuredProject.GetProjectLevelPropertyPagesCatalogAsync();
            }

            return null;
        }

        /// <summary>
        /// Retrieves the project-level <see cref="IPropertyPagesCatalog"/> for a <see cref="ConfiguredProject"/>.
        /// </summary>
        public static async Task<IPropertyPagesCatalog?> GetProjectLevelPropertyPagesCatalogAsync(this ConfiguredProject project)
        {
            if (project.Services.PropertyPagesCatalog is IPropertyPagesCatalogProvider catalogProvider)
            {
                return await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the <see cref="BaseProperty"/> of the given <paramref name="propertyName"/>, as well as the index
        /// of the property within its <see cref="Rule"/>.
        /// </summary>
        public static bool TryGetPropertyAndIndex(this Rule rule, string propertyName, [NotNullWhen(true)] out BaseProperty? property, out int index)
        {
            foreach ((var i, var prop) in rule.Properties.WithIndices())
            {
                if (StringComparers.PropertyNames.Equals(prop.Name, propertyName))
                {
                    property = prop;
                    index = i;
                    return true;
                }
            }

            property = null;
            index = default;
            return false;
        }
    }
}
