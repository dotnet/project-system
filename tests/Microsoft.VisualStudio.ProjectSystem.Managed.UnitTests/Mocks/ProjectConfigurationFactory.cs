// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectConfigurationFactory
    {
        public static ProjectConfiguration Create(string name, IImmutableDictionary<string, string> dimensions)
        {
            return new StandardProjectConfiguration(name, dimensions);
        }

        public static ProjectConfiguration Create(string dimensionNames, string dimensionValues)
        {
            var dimensionsBuilder = ImmutableDictionary.CreateBuilder<string, string>();

            string[] dimensionNamesArray = dimensionNames.Split('|');
            string[] dimensionValuesArray = dimensionValues.Split('|');

            for (int i = 0; i < dimensionNamesArray.Length; i++)
            {
                dimensionsBuilder.Add(dimensionNamesArray[i], dimensionValuesArray[i]);
            }

            return Create(dimensionValues, dimensionsBuilder.ToImmutable());
        }

        public static ProjectConfiguration Create(string configuration)
        {
            var dimensionsBuilder = ImmutableDictionary.CreateBuilder<string, string>();

            string[] dimensions = configuration.Split('|');

            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensionsBuilder.Add(GetDimensionName(i), dimensions[i]);
            }

            return Create(configuration, dimensionsBuilder.ToImmutable());
        }

        public static IReadOnlyList<ProjectConfiguration> CreateMany(params string[] configurations)
        {
            var configurationsBuilder = ImmutableArray.CreateBuilder<ProjectConfiguration>();

            foreach (string configuration in configurations)
            {
                ProjectConfiguration config = Create(configuration);
                configurationsBuilder.Add(config);
            }

            return configurationsBuilder.ToImmutable();
        }

        public static ProjectConfiguration FromJson(string jsonString)
        {
            var model = new ProjectConfigurationModel();
            return model.FromJson(jsonString);
        }

        private static string GetDimensionName(int ordinal)
        {
            return ordinal switch
            {
                0 => "Configuration",
                1 => "Platform",
                2 => "TargetFramework",

                _ => throw new InvalidOperationException(),
            };
        }
    }

    internal class ProjectConfigurationModel : JsonModel<ProjectConfiguration>
    {
        public IImmutableDictionary<string, string>? Dimensions { get; set; }
        public string? Name { get; set; }

        public override ProjectConfiguration ToActualModel()
        {
            Assumes.NotNull(Dimensions);
            Assumes.NotNull(Name);

            return new ActualModel(Dimensions, Name);
        }

        private class ActualModel : ProjectConfiguration
        {
            public IImmutableDictionary<string, string> Dimensions { get; }
            public string Name { get; }

            public ActualModel(IImmutableDictionary<string, string> dimensions, string name)
            {
                Dimensions = dimensions;
                Name = name;
            }

            public bool Equals(ProjectConfiguration? other)
            {
                throw new NotImplementedException();
            }
        }
    }
}
