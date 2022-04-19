// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IConfigurationGroupFactory
    {
        public static IConfigurationGroup<ProjectConfiguration> CreateFromConfigurationNames(params string[] configurationNames)
        {
            IEnumerable<ProjectConfiguration> configurations = configurationNames.Select(ProjectConfigurationFactory.Create);

            return Create(configurations);
        }

        public static IConfigurationGroup<T> Create<T>(IEnumerable<T> values)
        {
            var group = new ConfigurationGroup<T>();

            group.AddRange(values);

            return group;
        }

        private class ConfigurationGroup<T> : List<T>, IConfigurationGroup<T>
        {
            public IReadOnlyCollection<string> VariantDimensionNames => throw new System.NotImplementedException();
        }
    }
}
