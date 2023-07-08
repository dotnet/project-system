// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    internal static class IProjectImageProviderFactory
    {
        public static IProjectImageProvider Create()
        {
            return Mock.Of<IProjectImageProvider>();
        }

        public static IProjectImageProvider ImplementGetProjectImage(Func<string, ProjectImageMoniker?> action)
        {
            var mock = new Mock<IProjectImageProvider>();
            mock.Setup(p => p.GetProjectImage(It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static IProjectImageProvider ImplementGetProjectImage(string key, ProjectImageMoniker? moniker)
        {
            return ImplementGetProjectImage(k => k == key ? moniker : null);
        }
    }
}
