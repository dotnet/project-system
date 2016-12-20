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

        public static IMsBuildAccessor ImplementGetProjectXmlRunLocked(string xml, Func<bool, Func<Task>, Task> callback)
        {
            var mock = new Mock<IMsBuildAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(Task.FromResult(xml));
            mock.Setup(m => m.RunLockedAsync(It.IsAny<bool>(), It.IsAny<Func<Task>>())).Returns(callback);
            return mock.Object;
        }

        public static IMsBuildAccessor ImplementGetProjectXmlAndXmlChangedEvents(string xml, HandlerCallback subscribeCallback, HandlerCallback unsubscribeCallback)
        {
            var mock = new Mock<IMsBuildAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(Task.FromResult(xml));
            mock.Setup(m => m.SubscribeProjectXmlChangedEventAsync(It.IsAny<UnconfiguredProject>(), It.IsAny<EventHandler<ProjectXmlChangedEventArgs>>()))
                .Callback<UnconfiguredProject, EventHandler<ProjectXmlChangedEventArgs>>((proj, handler) => subscribeCallback(handler))
                .Returns(Task.CompletedTask);
            mock.Setup(m => m.UnsubscribeProjectXmlChangedEventAsync(It.IsAny<UnconfiguredProject>(), It.IsAny<EventHandler<ProjectXmlChangedEventArgs>>()))
               .Callback<UnconfiguredProject, EventHandler<ProjectXmlChangedEventArgs>>((proj, handler) => unsubscribeCallback(handler))
               .Returns(Task.CompletedTask);
            return mock.Object;
        }
    }
}
