// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IInterceptingPropertyValueProviderMetadataFactory
    {
        public static IInterceptingPropertyValueProviderMetadata Create(string propertyName)
        {
            var mock = new Mock<IInterceptingPropertyValueProviderMetadata>();

            mock.SetupGet(s => s.PropertyName)
                .Returns(propertyName);

            return mock.Object;
        }
    }
}
