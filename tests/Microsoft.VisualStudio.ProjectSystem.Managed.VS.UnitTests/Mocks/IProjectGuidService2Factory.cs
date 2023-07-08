// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectGuidService2Factory
    {
        public static IProjectGuidService ImplementGetProjectGuidAsync(Guid result)
        {
            return ImplementGetProjectGuidAsync(() => result);
        }

        public static IProjectGuidService ImplementGetProjectGuidAsync(Func<Guid> action)
        {
            var mock = new Mock<IProjectGuidService2>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(action);

            // All IProjectGuidService2 have to be IProjectGuidService instances
            return mock.As<IProjectGuidService>().Object;
        }
    }
}
