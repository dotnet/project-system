// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectPropertiesFactory
    {
        public static Mock<IProjectProperties> MockWithProperty(string propertyName)
        {
            return MockWithProperties(ImmutableArray.Create(propertyName));
        }

        public static Mock<IProjectProperties> MockWithProperties(IEnumerable<string> propertyNames)
        {
            var mock = new Mock<IProjectProperties>();

            mock.Setup(t => t.GetPropertyNamesAsync())
                .ReturnsAsync(propertyNames);

            return mock;
        }

        public static Mock<IProjectProperties> MockWithPropertyAndValue(string propertyName, string setValue)
        {
            return MockWithPropertiesAndValues(new Dictionary<string, string?>() { { propertyName, setValue } });
        }

        public static Mock<IProjectProperties> MockWithPropertiesAndValues(IDictionary<string, string?> propertyNameAndValues, HashSet<string>? inheritedPropertyNames = null)
        {
            var mock = MockWithProperties(propertyNameAndValues.Keys);

            // evaluated properties are never null
            mock.Setup(t => t.GetEvaluatedPropertyValueAsync(It.IsAny<string>()))
                .Returns<string>(k => Task.FromResult(propertyNameAndValues.TryGetValue(k, out var v) ? v ?? "" : ""));

            mock.Setup(t => t.GetUnevaluatedPropertyValueAsync(
                It.IsIn<string>(propertyNameAndValues.Keys)))
                 .Returns<string>(k => Task.FromResult(propertyNameAndValues[k]));

            mock.Setup(t => t.SetPropertyValueAsync(
                It.IsIn<string>(propertyNameAndValues.Keys),
                It.IsAny<string>(), null))
                 .Returns<string, string, IReadOnlyDictionary<string, string>>((k, v, d) =>
                    {
                        propertyNameAndValues[k] = v;

                        inheritedPropertyNames?.Remove(k);

                        return Task.CompletedTask;
                    });

            mock.Setup(t => t.DeletePropertyAsync(
                It.IsIn<string>(propertyNameAndValues.Keys),
                null))
                 .Returns<string, IReadOnlyDictionary<string, string>>((propName, d) =>
                    {
                        propertyNameAndValues[propName] = null;
                        return Task.CompletedTask;
                    });

            if (inheritedPropertyNames is not null)
            {
                mock.Setup(t => t.IsValueInheritedAsync(
                    It.IsIn<string>(propertyNameAndValues.Keys)))
                    .Returns<string>(k => Task.FromResult(inheritedPropertyNames.Contains(k)));
            }

            return mock;
        }

        public static IProjectProperties CreateWithProperty(string propertyName)
            => MockWithProperty(propertyName).Object;

        public static IProjectProperties CreateWithPropertyAndValue(string propertyName, string setValue)
            => MockWithPropertyAndValue(propertyName, setValue).Object;

        public static IProjectProperties CreateWithPropertiesAndValues(IDictionary<string, string?> propertyNameAndValues, HashSet<string>? inheritedPropertyNames = null)
            => MockWithPropertiesAndValues(propertyNameAndValues, inheritedPropertyNames).Object;
    }
}
