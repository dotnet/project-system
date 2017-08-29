// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        public static IProjectXmlAccessor ImplementGetProjectXml(string xml) => Implement(() => xml);

        public static IProjectXmlAccessor Implement(Func<string> xmlFunc)
        {
            var mock = new Mock<IProjectXmlAccessor>();
            mock.Setup(m => m.GetProjectXmlAsync()).Returns(() => Task.FromResult(xmlFunc()));
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

        public static IProjectXmlAccessor WithItems(string itemType, string metadataName, ICollection<(string name, string metadataValue)> items)
        {
            var mock = new Mock<IProjectXmlAccessor>();

            mock.Setup(m => m.GetItems(
                It.IsAny<ConfiguredProject>(), 
                It.Is<string>((t) => string.Equals(t, itemType)),
                It.Is<string>((t) => string.Equals(t, metadataName))))
                .Returns<ConfiguredProject, string, string>((configuredProject, innerItemType, innerMetadataName) =>
                {
                    return Task.FromResult(items);
                });

            return mock.Object;
        }

    }
}
