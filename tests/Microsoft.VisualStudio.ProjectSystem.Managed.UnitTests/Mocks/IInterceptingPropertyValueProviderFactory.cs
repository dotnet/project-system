// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

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

            if (onGetEvaluatedPropertyValue is not null)
            {
                mock.Setup(t => t.OnGetEvaluatedPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>()))
                     .Returns<string, string, IProjectProperties>((n, u, p) => Task.FromResult(onGetEvaluatedPropertyValue(u, p)));
            }

            if (onGetUnevaluatedPropertyValue is not null)
            {
                mock.Setup(t => t.OnGetUnevaluatedPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>()))
                     .Returns<string, string, IProjectProperties>((n, u, p) => Task.FromResult(onGetUnevaluatedPropertyValue(u, p)));
            }

            if (onSetPropertyValue is not null)
            {
                mock.Setup(t => t.OnSetPropertyValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IProjectProperties>(),
                    It.IsAny<IReadOnlyDictionary<string, string>>()))
                     .Returns<string, string, IProjectProperties, IReadOnlyDictionary<string, string>>((n, u, p, d) => Task.FromResult(onSetPropertyValue(u, p, d)));
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
