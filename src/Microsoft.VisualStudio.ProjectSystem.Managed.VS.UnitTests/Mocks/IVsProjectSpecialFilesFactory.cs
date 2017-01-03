// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsProjectSpecialFilesFactory
    {
        public static IVsProjectSpecialFiles ImplementGetFile(FuncWithOut<int, uint, uint, string, int> action)
        {
            var mock = new Mock<IVsHierarchy>().As<IVsProjectSpecialFiles>();

            uint itemId;
            string fileName;
            mock.Setup(s => s.GetFile(It.IsAny<int>(), It.IsAny<uint>(), out itemId, out fileName))
                .Returns(action);

            return mock.Object;
        }
    }
}
