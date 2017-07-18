// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsFileChangeExFactory
    {
        public static IVsFileChangeEx CreateWithAdviseUnadviseFileChange(uint adviseCookie)
        {
            var mock = new Mock<IVsFileChangeEx>();
            mock.Setup(x => x.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IVsFileChangeEvents>(), out adviseCookie)).Returns(VSConstants.S_OK);
            mock.Setup(x => x.UnadviseFileChange(It.IsAny<uint>())).Returns(VSConstants.S_OK);
            return mock.Object;
        }
    }
}
