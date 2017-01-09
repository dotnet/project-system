// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextBufferFactory
    {
        public static ITextBuffer Create() => Mock.Of<ITextBuffer>();

        public static ITextBuffer ImplementSnapshot(string text)
        {
            var mock = new Mock<ITextBuffer>();
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(s => s.GetText()).Returns(text);
            mock.SetupGet(t => t.CurrentSnapshot).Returns(snapshotMock.Object);
            return mock.Object;
        }
    }
}
