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

        public static IProjectXmlAccessor ImplementGetProjectXml(string xml) => Implement(() => xml, s => { });

        public static IProjectXmlAccessor Implement(Func<string> xmlFunc, Action<string> saveCallback)
        {
            var mock = new Mock<IProjectXmlAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(() => Task.FromResult(xmlFunc()));
            mock.Setup(m => m.SaveProjectXmlAsync(It.IsAny<string>())).Callback(saveCallback).Returns(Task.CompletedTask);
            return mock.Object;
        }
    }
}
