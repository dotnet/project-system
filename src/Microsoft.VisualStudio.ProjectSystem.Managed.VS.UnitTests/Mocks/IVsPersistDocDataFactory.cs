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
    }
}
