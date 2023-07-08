// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectDesignerServiceFactory
    {
        public static IProjectDesignerService Create()
        {
            return Mock.Of<IProjectDesignerService>();
        }

        public static IProjectDesignerService ImplementSupportsProjectDesigner(Func<bool> action)
        {
            var mock = new Mock<IProjectDesignerService>();

            mock.SetupGet(f => f.SupportsProjectDesigner)
                .Returns(action);

            return mock.Object;
        }

        public static IProjectDesignerService ImplementShowProjectDesignerAsync(Action action)
        {
            var mock = new Mock<IProjectDesignerService>();
            mock.Setup(s => s.ShowProjectDesignerAsync())
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
