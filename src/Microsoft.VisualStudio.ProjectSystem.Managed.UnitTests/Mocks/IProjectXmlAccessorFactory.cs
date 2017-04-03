// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
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

        public static IProjectXmlAccessor Create(ProjectRootElement msbuildProject)
        {
            var mock = new Mock<IProjectXmlAccessor>();

            mock.Setup(m => m.GetEvaluatedPropertyValue(It.IsAny<UnconfiguredProject>(), It.IsAny<string>()))
                .Returns<UnconfiguredProject, string>((unconfiguredProject, propertyName) =>
                {
                    // Return the value from the msbuild project directly to avoid mocking the configured project
                    var property = msbuildProject.Properties
                        .Where(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();
                    return Task.FromResult(property?.Value);
                });

            mock.Setup(m => m.ExecuteInWriteLock(It.IsAny<Action<ProjectRootElement>>()))
                .Returns<Action<ProjectRootElement>>((action) =>
                {
                    action(msbuildProject);
                    return Task.CompletedTask;
                });

            return mock.Object;
        }
    }
}
