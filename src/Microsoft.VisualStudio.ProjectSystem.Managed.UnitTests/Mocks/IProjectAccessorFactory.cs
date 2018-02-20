// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectAccessorFactory
    {
        public static IProjectAccessor Create()
        {
            return Mock.Of<IProjectAccessor>();
        }

        public static IProjectAccessor ImplementOpenProjectForReadAsync<TResult>(string xml)
        {
            var rootElement = ProjectRootElementFactory.Create(xml);
            var evaluationProject = ProjectFactory.Create(rootElement);

            var mock = new Mock<IProjectAccessor>();
            mock.Setup(a => a.OpenProjectForReadAsync(It.IsAny<ConfiguredProject>(), It.IsAny<Func<Project, TResult>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ConfiguredProject project, Func<Project, TResult> action, CancellationToken cancellationToken) =>
                {
                    return action(evaluationProject);
                });

            return mock.Object;
        }
    }
}
