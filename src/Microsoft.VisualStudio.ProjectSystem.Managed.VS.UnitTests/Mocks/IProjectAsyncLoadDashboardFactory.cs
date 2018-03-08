// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectAsyncLoadDashboardFactory
    {
        public static IProjectAsyncLoadDashboard ImplementProjectLoadedInHost(Func<Task> action)
        {
            var mock = new Mock<IProjectAsyncLoadDashboard>();

            mock.SetupGet(s => s.ProjectLoadedInHost)
                .Returns(action);

            return mock.Object;
        }
    }
}
