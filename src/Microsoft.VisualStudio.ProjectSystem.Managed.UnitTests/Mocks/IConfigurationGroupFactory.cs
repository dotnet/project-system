// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IConfigurationGroupFactory
    {
        public static IConfigurationGroup<ProjectConfiguration> CreateFromConfigurationNames(params string[] configurationNames)
        {
            IEnumerable<ProjectConfiguration> configurations = configurationNames.Select(name => ProjectConfigurationFactory.Create(name));

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
