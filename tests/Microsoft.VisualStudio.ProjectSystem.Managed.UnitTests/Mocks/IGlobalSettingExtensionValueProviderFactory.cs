// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class IGlobalSettingExtensionValueProviderFactory
    {
        public static IGlobalSettingExtensionValueProvider Create(
            Func<string, ImmutableDictionary<string, object>, Rule?, string>? onGetPropertyValue = null,
            Func<string, string, ImmutableDictionary<string, object>, Rule?, ImmutableDictionary<string, object?>>? onSetPropertyValue = null)
        {
            var providerMock = new Mock<IGlobalSettingExtensionValueProvider>();

            if (onGetPropertyValue is not null)
            {
                providerMock.Setup(t => t.OnGetPropertyValue(It.IsAny<string>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onGetPropertyValue);
            }

            if (onSetPropertyValue is not null)
            {
                providerMock.Setup(t => t.OnSetPropertyValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onSetPropertyValue);
            }

            return providerMock.Object;
        }
    }
}
