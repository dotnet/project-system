// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Moq;
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    internal static class IMsBuildAccessorFactory
    {
        public static IMsBuildAccessor Create() => Mock.Of<IMsBuildAccessor>();

        public static IMsBuildAccessor Implement(string xml, Func<bool, Func<Task>, Task> callback)
        {
            var mock = new Mock<IMsBuildAccessor>();
            mock.Setup(m => m.GetProjectXml(It.IsAny<UnconfiguredProject>())).Returns(Task.FromResult(xml));
            mock.Setup(m => m.RunLockedAsync(It.IsAny<bool>(), It.IsAny<Func<Task>>())).Returns(callback);
            return mock.Object;
        }
    }
}
