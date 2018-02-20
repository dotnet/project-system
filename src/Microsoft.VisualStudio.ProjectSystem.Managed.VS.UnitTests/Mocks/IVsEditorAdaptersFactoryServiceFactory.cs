// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using Moq;

namespace Microsoft.VisualStudio.Editor
{
    internal static class IVsEditorAdaptersFactoryServiceFactory
    {
        public static IVsEditorAdaptersFactoryService Create() => Mock.Of<IVsEditorAdaptersFactoryService>();

        public static IVsEditorAdaptersFactoryService ImplementGetDocumentBuffer(ITextBuffer buffer)
        {
            var mock = new Mock<IVsEditorAdaptersFactoryService>();
            mock.Setup(e => e.GetDocumentBuffer(It.IsAny<IVsTextBuffer>())).Returns(buffer);
            return mock.Object;
        }
    }
}
