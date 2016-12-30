// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal static class IProjectXmlAccessorFactory
    {
        public delegate void HandlerCallback(EventHandler<ProjectXmlChangedEventArgs> callback);

        public static IProjectXmlAccessor Create() => Mock.Of<IProjectXmlAccessor>();

        public static IProjectXmlAccessor ImplementGetProjectXml(string xml) => ImplementGetProjectXml(() => xml);

        public static IProjectXmlAccessor ImplementGetProjectXml(Func<string> xmlFunc)
        {
            var mock = new Mock<IProjectXmlAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(() => Task.FromResult(xmlFunc()));
            return mock.Object;
        }
    }
}
