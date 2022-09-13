// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPropertyPagesCatalogProviderFactory
    {
        public static IPropertyPagesCatalogProvider Create(Dictionary<string, IPropertyPagesCatalog> catalogsByContext, IPropertyPagesCatalog? memoryOnlyCatalog = null)
        {
            var catalogProvider = new Mock<IPropertyPagesCatalogProvider>();
            catalogProvider
                .Setup(o => o.GetCatalogsAsync(CancellationToken.None))
                .ReturnsAsync(catalogsByContext.ToImmutableDictionary());

            catalogProvider
                .Setup(o => o.GetCatalogAsync(It.IsAny<string>(), CancellationToken.None))
                .Returns((string name, CancellationToken token) => Task.FromResult(catalogsByContext[name]));

            if (memoryOnlyCatalog is not null)
            {
                catalogProvider
                    .Setup(o => o.GetMemoryOnlyCatalog(It.IsAny<string>()))
                    .Returns(memoryOnlyCatalog);
            }

            return catalogProvider.Object;
        }
    }
}
