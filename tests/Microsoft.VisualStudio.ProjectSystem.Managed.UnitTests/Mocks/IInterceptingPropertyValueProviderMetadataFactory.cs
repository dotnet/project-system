// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IInterceptingPropertyValueProviderMetadataFactory
    {
        public static IInterceptingPropertyValueProviderMetadata Create(string propertyName)
        {
            var mock = new Mock<IInterceptingPropertyValueProviderMetadata>();

            mock.SetupGet(s => s.PropertyNames)
                .Returns(new[] { propertyName });

            return mock.Object;
        }
    }
}
