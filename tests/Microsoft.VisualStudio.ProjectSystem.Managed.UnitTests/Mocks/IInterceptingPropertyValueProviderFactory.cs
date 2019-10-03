// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IInterceptingPropertyValueProviderFactory
    {
        public static IInterceptingPropertyValueProvider Create(
            Func<string, IProjectProperties, string>? onGetEvaluatedPropertyValue = null,
            Func<string, IProjectProperties, string>? onGetUnevaluatedPropertyValue = null,
            Func<string, IProjectProperties, IReadOnlyDictionary<string, string>, string?>? onSetPropertyValue = null)
        {
            var mock = new Mock<IInterceptingPropertyValueProvider>();

            if (onGetEvaluatedPropertyValue != null)
            {
                mock.Setup(t => t.OnGetEvaluatedPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>()))
                     .Returns<string, IProjectProperties>((u, p) => Task.FromResult(onGetEvaluatedPropertyValue(u, p)));
            }

            if (onGetUnevaluatedPropertyValue != null)
            {
                mock.Setup(t => t.OnGetUnevaluatedPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>()))
                     .Returns<string, IProjectProperties>((u, p) => Task.FromResult(onGetUnevaluatedPropertyValue(u, p)));
            }

            if (onSetPropertyValue != null)
            {
                mock.Setup(t => t.OnSetPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>(),
                    It.IsAny<IReadOnlyDictionary<string, string>>()))
                     .Returns<string, IProjectProperties, IReadOnlyDictionary<string, string>>((u, p, d) => Task.FromResult(onSetPropertyValue(u, p, d)));
            }

            return mock.Object;
        }

        public static Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> Create(
            string propertyName,
            Func<string, IProjectProperties, string>? onGetEvaluatedPropertyValue = null,
            Func<string, IProjectProperties, string>? onGetUnevaluatedPropertyValue = null,
            Func<string, IProjectProperties, IReadOnlyDictionary<string, string>, string?>? onSetPropertyValue = null)
        {
            var mockMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName);
            var mockProvider = Create(onGetEvaluatedPropertyValue, onGetUnevaluatedPropertyValue, onSetPropertyValue);
            return new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(() => mockProvider, mockMetadata);
        }
    }
}
