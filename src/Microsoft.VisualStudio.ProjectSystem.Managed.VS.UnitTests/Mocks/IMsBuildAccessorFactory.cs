// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    internal static class IMsBuildAccessorFactory
    {
        public delegate void HandlerCallback(EventHandler<ProjectXmlChangedEventArgs> callback);

        public static IMsBuildAccessor Create() => Mock.Of<IMsBuildAccessor>();

        public static IMsBuildAccessor ImplementGetProjectXml(string xml) => ImplementGetProjectXml(() => xml);

        public static IMsBuildAccessor ImplementGetProjectXml(Func<string> xmlFunc)
        {
            var mock = new Mock<IMsBuildAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(() => Task.FromResult(xmlFunc()));
            return mock.Object;
        }
    }
}
