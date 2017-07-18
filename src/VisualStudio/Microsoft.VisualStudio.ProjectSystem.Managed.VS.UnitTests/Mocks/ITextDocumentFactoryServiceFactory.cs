// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextDocumentFactoryServiceFactory
    {
        public static ITextDocumentFactoryService Create() => Mock.Of<ITextDocumentFactoryService>();

        public static ITextDocumentFactoryService ImplementGetTextDocument(ITextDocument doc, bool success)
        {
            var mock = new Mock<ITextDocumentFactoryService>();
            mock.Setup(t => t.TryGetTextDocument(It.IsAny<ITextBuffer>(), out doc)).Returns(success);
            return mock.Object;
        }
    }
}
