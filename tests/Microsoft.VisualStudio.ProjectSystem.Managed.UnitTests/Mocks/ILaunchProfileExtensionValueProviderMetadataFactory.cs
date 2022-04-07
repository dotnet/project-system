// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class ILaunchProfileExtensionValueProviderMetadataFactory
    {
        public static ILaunchProfileExtensionValueProviderMetadata Create(string[]? propertyNames = null)
        {
            var metadataMock = new Mock<ILaunchProfileExtensionValueProviderMetadata>();

            if (propertyNames is not null)
            {
                metadataMock.Setup(t => t.PropertyNames).Returns(propertyNames);
            }

            return metadataMock.Object;
        }

        public static ILaunchProfileExtensionValueProviderMetadata Create(string propertyName)
        {
            return Create(new[] { propertyName });
        }
    }
}
