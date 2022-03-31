// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ITemporaryPropertyStorageFactory
    {
        public static ITemporaryPropertyStorage Create(Dictionary<string, string>? values = null)
        {
            values ??= new();

            var mock = new Mock<ITemporaryPropertyStorage>();

            mock.Setup(o => o.AddOrUpdatePropertyValue(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((name, value) => values[name] = value);

            mock.Setup(o => o.GetPropertyValue(It.IsAny<string>()))
                .Returns<string>(name => { values.TryGetValue(name, out string value); return value; });

            return mock.Object;
        }
    }
}
