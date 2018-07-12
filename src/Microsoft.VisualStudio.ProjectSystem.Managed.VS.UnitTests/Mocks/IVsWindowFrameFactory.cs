// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsWindowFrameFactory
    {
        public static IVsWindowFrame Create() => Mock.Of<IVsWindowFrame>();

        public static IVsWindowFrame ImplementShow(Func<int> action)
        {
            var mock = new Mock<IVsWindowFrame>();
            mock.Setup(h => h.Show())
                .Returns(action());

            return mock.Object;
        }

        public static IVsWindowFrame ImplementShowAndSetProperty(int showRetVal, Func<int, object, int> setPropertyAction)
        {
            var mock = new Mock<IVsWindowFrame>();
            mock.Setup(w => w.Show()).Returns(showRetVal);
            mock.Setup(w => w.SetProperty(It.IsAny<int>(), It.IsAny<object>())).Returns(setPropertyAction);
            return mock.Object;
        }

        public static IVsWindowFrame ImplementCloseFrame(Func<uint, int> callback)
        {
            var mock = new Mock<IVsWindowFrame>();
            mock.Setup(w => w.CloseFrame(It.IsAny<uint>())).Returns(callback);
            return mock.Object;
        }
    }
}
