// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsOutputWindowPaneFactory
    {
        public static IVsOutputWindowPane Create()
        {
            var mock = new Mock<IVsOutputWindowPane>();
            return mock.Object;
        }

        public static IVsOutputWindowPane ImplementOutputStringThreadSafe(Action<string> action)
        {
            var mock = new Mock<IVsOutputWindowPane>();
            mock.Setup(p => p.OutputStringThreadSafe(It.IsAny<string>()))
                .Callback(action)
                .Returns(0);

            return mock.Object;
        }

        public static IVsOutputWindowPane ImplementActivate(Func<int> action)
        {
            var mock = new Mock<IVsOutputWindowPane>();
            mock.Setup(p => p.Activate())
                .Returns(action);

            return mock.Object;
        }
    }
}
