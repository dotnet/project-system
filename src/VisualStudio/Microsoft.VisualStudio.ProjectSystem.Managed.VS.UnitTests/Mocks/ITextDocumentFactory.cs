// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextDocumentFactory
    {
        public static ITextDocument Create() => Mock.Of<ITextDocument>();

        public static ITextDocument ImplementTextBuffer(ITextBuffer buffer)
        {
            var mock = new Mock<ITextDocument>();
            mock.SetupGet(t => t.TextBuffer).Returns(buffer);
            return mock.Object;
        }
    }
}
