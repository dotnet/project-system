// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IConfigurationGroupFactory
    {
        public static IConfigurationGroup<ProjectConfiguration> CreateFromConfigurationNames(params string[] configurationNames)
        {
            IEnumerable<StandardProjectConfiguration> configurations = configurationNames.Select(name => new StandardProjectConfiguration(name, ImmutableDictionary<string, string>.Empty));

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
            public ConfigurationGroup()
            {
            }

            public IReadOnlyCollection<string> VariantDimensionNames => throw new System.NotImplementedException();
        }
    }
}
