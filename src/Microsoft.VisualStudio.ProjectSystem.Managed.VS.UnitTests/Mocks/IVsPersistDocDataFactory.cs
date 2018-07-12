// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TextManager.Interop;

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsPersistDocDataFactory
    {
        public static IVsPersistDocData Create() => Mock.Of<IVsPersistDocData>();

        public static IVsPersistDocData ImplementAsIVsTextBuffer()
        {
            var mock = new Mock<IVsPersistDocData>();
            mock.As<IVsTextBuffer>();
            return mock.Object;
        }

        public static IVsPersistDocData ImplementAsIVsTextBufferIsDocDataDirty(bool isDirty, int ret)
        {
            var mock = Mock.Get(ImplementAsIVsTextBuffer());
            int isDirtyOut = 0;
            mock.Setup(m => m.IsDocDataDirty(out isDirtyOut)).Returns((out int param) =>
            {
                param = isDirty ? 1 : 0;
                return ret;
            });
            return mock.Object;
        }

        public static IVsPersistDocData ImplementAsIVsTextBufferGetStateFlags(uint existingFlags)
        {
            var mock = new Mock<IVsPersistDocData>();
            var textBufferMock = mock.As<IVsTextBuffer>();
            uint flags = 0;
            textBufferMock.Setup(t => t.GetStateFlags(out flags)).Returns((out uint f) =>
            {
                f = existingFlags;
                return VSConstants.S_OK;
            });

            return mock.Object;
        }
    }
}
