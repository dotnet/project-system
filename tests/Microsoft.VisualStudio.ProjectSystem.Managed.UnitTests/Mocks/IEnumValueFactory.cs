// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IEnumValueFactory
    {
        public static IEnumValue Create(string? displayName = null, string? name = null)
        {
            var mock = new Mock<IEnumValue>();

            if (displayName is not null)
            {
                mock.SetupGet(m => m.DisplayName).Returns(displayName);
            }

            if (name is not null)
            {
                mock.SetupGet(m => m.Name).Returns(name);
            }

            return mock.Object;
        }
    }
}
