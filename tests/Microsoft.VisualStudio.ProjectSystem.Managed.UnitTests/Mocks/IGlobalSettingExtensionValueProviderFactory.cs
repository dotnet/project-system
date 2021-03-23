// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class IGlobalSettingExtensionValueProviderFactory
    {
        public static IGlobalSettingExtensionValueProvider Create(
            Func<string, ImmutableDictionary<string, object>, Rule?, Task<string>>? onGetPropertyValueAsync = null,
            Func<string, string, ImmutableDictionary<string, object>, Rule?, Task<ImmutableDictionary<string, object>>>? onSetPropertyValueAsync = null)
        {
            var providerMock = new Mock<IGlobalSettingExtensionValueProvider>();

            if (onGetPropertyValueAsync is not null)
            {
                providerMock.Setup(t => t.OnGetPropertyValueAsync(It.IsAny<string>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onGetPropertyValueAsync);
            }

            if (onSetPropertyValueAsync is not null)
            {
                providerMock.Setup(t => t.OnSetPropertyValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onSetPropertyValueAsync);
            }

            return providerMock.Object;
        }
    }
}
