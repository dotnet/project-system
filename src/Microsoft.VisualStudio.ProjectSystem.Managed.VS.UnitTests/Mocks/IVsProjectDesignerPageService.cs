// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

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
