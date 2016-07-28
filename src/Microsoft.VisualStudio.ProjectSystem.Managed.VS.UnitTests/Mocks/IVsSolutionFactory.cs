// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsSolutionFactory
    {
        public static IVsSolution CreateWithAdviseUnadviseSolutionEvents(uint adviseCookie)
        {
            var mock = new Mock<IVsSolution>();
            mock.Setup(x => x.AdviseSolutionEvents(It.IsAny<IVsSolutionEvents>(), out adviseCookie)).Returns(VSConstants.S_OK);
            mock.Setup(x => x.UnadviseSolutionEvents(It.IsAny<uint>())).Returns(VSConstants.S_OK);
            return mock.Object;
        }
    }
}
