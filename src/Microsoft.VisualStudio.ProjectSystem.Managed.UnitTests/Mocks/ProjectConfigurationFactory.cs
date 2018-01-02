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
}
