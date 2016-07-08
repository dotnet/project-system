// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectPropertiesFactory
    {
        public static Mock<IProjectProperties> MockWithProperty(string propertyName)
        {
            var mock = new Mock<IProjectProperties>();

            IEnumerable<string> names = new string[] { propertyName };
            mock.Setup(t => t.GetPropertyNamesAsync())
                .Returns(Task.FromResult(names));

            return mock;
        }

        public static Mock<IProjectProperties> MockWithPropertyAndSet(string propertyName, string setValue)
        {
            var mock = MockWithProperty(propertyName);

            mock.Setup(t => t.SetPropertyValueAsync(
                    It.Is<string>(v => v == propertyName), 
                    It.Is<string>(v => v == setValue), null))
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static IProjectProperties CreateWithProperty(string propertyName) 
            => MockWithProperty(propertyName).Object;

        public static IProjectProperties CreateWithPropertyAndSet(string propertyName, string setValue) 
            => MockWithPropertyAndSet(propertyName, setValue).Object;
    }
}
