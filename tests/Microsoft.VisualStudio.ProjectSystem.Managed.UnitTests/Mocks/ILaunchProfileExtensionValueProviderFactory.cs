// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class ILaunchProfileExtensionValueProviderFactory
    {
        public static ILaunchProfileExtensionValueProvider Create(
            Func<string, ILaunchProfile, ImmutableDictionary<string, object>, Rule?, string>? onGetPropertyValue = null,
            Action<string, string, IWritableLaunchProfile, ImmutableDictionary<string, object>, Rule?>? onSetPropertyValue = null)
        {
            var providerMock = new Mock<ILaunchProfileExtensionValueProvider>();

            if (onGetPropertyValue is not null)
            {
                providerMock.Setup(t => t.OnGetPropertyValue(It.IsAny<string>(), It.IsAny<ILaunchProfile>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onGetPropertyValue);
            }

            if (onSetPropertyValue is not null)
            {
                providerMock.Setup(t => t.OnSetPropertyValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWritableLaunchProfile>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Callback(onSetPropertyValue);
            }

            return providerMock.Object;
        }
    }
}
