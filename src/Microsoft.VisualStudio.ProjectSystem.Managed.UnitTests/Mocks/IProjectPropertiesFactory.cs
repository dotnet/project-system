// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectPropertiesFactory
    {
        public static Mock<IProjectProperties> MockWithProperty(string propertyName)
        {
            return MockWithProperties(ImmutableArray.Create(propertyName));
        }

        public static Mock<IProjectProperties> MockWithProperties(IEnumerable<string> propertyNames)
        {
            var mock = new Mock<IProjectProperties>();

            mock.Setup(t => t.GetPropertyNamesAsync())
                .Returns(Task.FromResult(propertyNames));

            return mock;
        }

        public static Mock<IProjectProperties> MockWithPropertyAndSet(string propertyName, string setValue)
        {
            return MockWithPropertiesAndSet(new Dictionary<string, string>() { { propertyName, setValue } });
        }

        public static Mock<IProjectProperties> MockWithPropertiesAndSet(Dictionary<string, string> propertyNameAndValues)
        {
            var mock = MockWithProperties(propertyNameAndValues.Keys);

            mock.Setup(t => t.SetPropertyValueAsync(
                It.IsIn<string>(propertyNameAndValues.Keys),
                It.IsAny<string>(), null))
                 .Returns<string, string, IReadOnlyDictionary<string, string>>((k, v, d) =>
                    Task.Run(() =>
                    {
                        propertyNameAndValues[k] = v;
                    }));

            return mock;
        }

        public static Mock<IProjectProperties> MockWithPropertiesAndGetSet(Dictionary<string, string> propertyNameAndValues)
        {
            var mock = MockWithPropertiesAndSet(propertyNameAndValues);

            mock.Setup(t => t.GetEvaluatedPropertyValueAsync(
                It.IsIn<string>(propertyNameAndValues.Keys)))
                 .Returns<string>(k => Task.FromResult(propertyNameAndValues[k]));

            mock.Setup(t => t.GetUnevaluatedPropertyValueAsync(
                It.IsIn<string>(propertyNameAndValues.Keys)))
                 .Returns<string>(k => Task.FromResult(propertyNameAndValues[k]));

            return mock;
        }

        public static IProjectProperties CreateWithProperty(string propertyName)
            => MockWithProperty(propertyName).Object;

        public static IProjectProperties CreateWithPropertyAndSet(string propertyName, string setValue)
            => MockWithPropertyAndSet(propertyName, setValue).Object;
    }
}
