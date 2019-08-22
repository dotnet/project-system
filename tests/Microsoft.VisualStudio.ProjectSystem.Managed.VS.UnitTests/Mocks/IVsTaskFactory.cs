// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsTaskFactory
    {
        public static IVsTask FromResult(object? result)
        {
            var mock = new Mock<IVsTask>();

            mock.Setup(t => t.IsCompleted)
                .Returns(true);
            mock.Setup<object?>(t => t.GetResult())
                .Returns(result);

            return mock.Object;
        }
    }
}
