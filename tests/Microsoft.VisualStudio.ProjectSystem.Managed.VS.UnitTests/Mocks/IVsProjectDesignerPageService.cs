// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IVsProjectDesignerPageServiceFactory
    {
        public static IVsProjectDesignerPageService Create()
        {
            return Mock.Of<IVsProjectDesignerPageService>();
        }

        public static IVsProjectDesignerPageService ImplementIsProjectDesignerSupported(Func<bool> action)
        {
            var mock = new Mock<IVsProjectDesignerPageService>();
            mock.SetupGet(s => s.IsProjectDesignerSupported)
                .Returns(action);

            return mock.Object;
        }
    }
}
