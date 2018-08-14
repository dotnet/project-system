// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
            switch (ordinal)
            {
                case 0:
                    return "Configuration";

                case 1:
                    return "Platform";

                case 2:
                    return "TargetFramework";

                default:
                    throw new InvalidOperationException();
            }
        }
    }

    internal class ProjectConfigurationModel : JsonModel<ProjectConfiguration>
    {
        public IImmutableDictionary<string, string> Dimensions { get; set; }

        public string Name { get; set; }

        public override ProjectConfiguration ToActualModel()
        {
            return new ActualModel
            {
                Dimensions = Dimensions,
                Name = Name
            };
        }

        private class ActualModel : ProjectConfiguration
        {
            public IImmutableDictionary<string, string> Dimensions { get; set; }

            public string Name { get; set; }

            public bool Equals(ProjectConfiguration other)
            {
                throw new NotImplementedException();
            }
        }
    }
}
