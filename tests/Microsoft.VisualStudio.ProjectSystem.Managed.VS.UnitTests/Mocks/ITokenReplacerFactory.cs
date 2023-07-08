// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class IDebugTokenReplacerFactory
    {
        public static IDebugTokenReplacer Create()
        {
            var mock = new Mock<IDebugTokenReplacer>();
            mock.Setup(s => s.ReplaceTokensInProfileAsync(It.IsAny<ILaunchProfile>()))
                .Returns<ILaunchProfile>(p => Task.FromResult(p));

            return mock.Object;
        }
    }
}
