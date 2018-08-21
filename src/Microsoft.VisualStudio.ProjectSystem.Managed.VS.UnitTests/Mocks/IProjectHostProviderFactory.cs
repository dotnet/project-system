// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IProjectHostProviderFactory
    {
        public static IProjectHostProvider Create()
        {
            return ImplementActiveIntellisenseProjectHostObject(null);
        }

        public static IProjectHostProvider ImplementActiveIntellisenseProjectHostObject(IConfiguredProjectHostObject hostObject)
        {
            var hostObjectMock = new Mock<IUnconfiguredProjectHostObject>();
            hostObjectMock.Setup(o => o.ActiveIntellisenseProjectHostObject)
                      .Returns(hostObject);

            var mock = new Mock<IProjectHostProvider>();
            mock.Setup(p => p.UnconfiguredProjectHostObject)
                .Returns(hostObjectMock.Object);

            return mock.Object;
        }
    }
}
