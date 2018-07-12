// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsUIShell7Factory
    {
        public static IVsUIShell7 ImplementAdviseWindowEvents(Func<IVsWindowFrameEvents, uint> adviseCallback)
        {
            var mock = new Mock<IVsUIShell7>();
            mock.As<IVsUIShell>();
            mock.Setup(s => s.AdviseWindowFrameEvents(It.IsAny<IVsWindowFrameEvents>())).Returns(adviseCallback);
            return mock.Object;
        }

        public static IVsUIShell7 ImplementAdviseUnadviseWindowEvents(Func<IVsWindowFrameEvents, uint> adviseCallback, Action<uint> unadviseCallback)
        {
            var mock = new Mock<IVsUIShell7>();
            mock.As<IVsUIShell>();
            mock.Setup(s => s.AdviseWindowFrameEvents(It.IsAny<IVsWindowFrameEvents>())).Returns(adviseCallback);
            mock.Setup(s => s.UnadviseWindowFrameEvents(It.IsAny<uint>())).Callback(unadviseCallback);
            return mock.Object;
        }
    }
}
