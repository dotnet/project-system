// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal static class ITextBufferManagerFactory
    {
        public static ITextBufferManager Create() => Mock.Of<ITextBufferManager>();

        public static ITextBufferManager ImplementFilePath(string path)
        {
            var mock = new Mock<ITextBufferManager>();
            mock.SetupGet(t => t.FilePath).Returns(path);
            return mock.Object;
        }
    }
}
