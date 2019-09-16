// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;

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
