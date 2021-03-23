// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class ILaunchProfileExtensionValueProviderFactory
    {
        public static ILaunchProfileExtensionValueProvider Create(
            Func<string, ILaunchProfile, ImmutableDictionary<string, object>, Rule?, Task<string>>? onGetPropertyValueAsync = null,
            Func<string, string, IWritableLaunchProfile, ImmutableDictionary<string, object>, Rule?, Task>? onSetPropertyValueAsync = null)
        {
            var providerMock = new Mock<ILaunchProfileExtensionValueProvider>();

            if (onGetPropertyValueAsync is not null)
            {
                providerMock.Setup(t => t.OnGetPropertyValueAsync(It.IsAny<string>(), It.IsAny<ILaunchProfile>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onGetPropertyValueAsync);
            }

            if (onSetPropertyValueAsync is not null)
            {
                providerMock.Setup(t => t.OnSetPropertyValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWritableLaunchProfile>(), It.IsAny<ImmutableDictionary<string, object>>(), It.IsAny<Rule?>()))
                    .Returns(onSetPropertyValueAsync);
            }

            return providerMock.Object;
        }
    }
}
