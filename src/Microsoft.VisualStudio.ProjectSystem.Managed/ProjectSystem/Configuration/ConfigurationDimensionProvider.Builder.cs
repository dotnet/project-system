// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal partial class ConfigurationDimensionProvider
    {
        private static IDimensionValues[] GuessDimensionsDefaultValue(ImmutableArray<ProjectPropertyElement> properties)
        {
            // Ideally we would have been passed just the property groups, but the CPS API provides only properties,
            // so we basically use those properties to get back to the property groups.

            IEnumerable<ProjectPropertyGroupElement> propertyGroups = properties.Select(p => p.Parent)
                                                                                .OfType<ProjectPropertyGroupElement>()
                                                                                .Distinct();

            return GuessDimensionsDefaultValue(propertyGroups, EmptyDimensions);
        }

        private static IDimensionValues[] GuessDimensionsDefaultValue(IEnumerable<ProjectPropertyGroupElement> propertyGroups, IImmutableDictionary<string, string> solutionConfiguration)
        {
            // NOTE: We try to somewhat mimic evaluation, but it doesn't have to be exact; its just
            // a guess at what "might" be the default configuration, not what it actually is.

            (ConditionalElement<ProjectPropertyGroupElement>, ConditionalElement<ProjectPropertyElement>[])[]? groups =
                propertyGroups.Select(group => (ToConditionalElement(group, group.Condition), group.Properties.Select(p => ToConditionalElement(p, p.Condition)).ToArray()))
                              .ToArray();

            GuessedDimensionValueBuilder[] builders = CreateValueBuilders();

            foreach (GuessedDimensionValueBuilder builder in builders)
            {
                if (TryGuessDeclaredDimension(builder, groups))
                    continue;

                if (TryGuessImplicitDimension(builder, groups))
                    continue;

                if (TryGuessSingularDimension(builder, groups))
                    continue;

                if (TrySolutionConfiguration(builder, solutionConfiguration))
                    continue;

                SetDefaultValues(builder);
            }

            return builders;
        }

        private static ConditionalElement<T> ToConditionalElement<T>(T element, string condition)
        {
            return new ConditionalElement<T>(element, condition);
        }

        public static bool HasWellKnownConditionThatAlwaysEvaluateToTrue(string condition)
        {
            return string.IsNullOrWhiteSpace(condition) ||
                StringComparers.PropertyLiteralValues.Equals(condition, "true");
        }

        private static bool HasIgnorableCondition<T>(ConditionalElement<T> element, string propertyName)
        {
            // We look for known conditions that evaluate to true so that projects can have patterns
            // where they opt in/out of configurations, platforms and target frameworks based on
            // whether they are inside Visual Studio or not.

            // For example:
            // 
            // <TargetFrameworks>net461;net452</TargetFrameworks>
            // <TargetFrameworks Condition = "'$(BuildingInsideVisualStudio)' == 'true'">net461</TargetFrameworks>

            if (HasWellKnownConditionThatAlwaysEvaluateToTrue(element.Condition))
                return true;

            IReadOnlyDictionary<string, string> conditionalProperties = element.ConditionalProperties;
            if (conditionalProperties.Count == 1)
            {
                // Handles "<Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>", common pattern in legacy
                if (conditionalProperties.TryGetValue(propertyName, out string value))
                    return value.Length == 0;

                // '$(OS)' == 'Windows_NT'
                if (conditionalProperties.TryGetValue("OS", out value))
                    return StringComparers.PropertyLiteralValues.Equals(value, "Windows_NT");

                // '$(BuildingInsideVisualStudio)' == 'true'
                if (conditionalProperties.TryGetValue("BuildingInsideVisualStudio", out value))
                    return StringComparers.PropertyLiteralValues.Equals(value, "true");
            }

            // Complex condition, we should skip this element
            return false;
        }

        private static bool TryGuessDeclaredDimension(GuessedDimensionValueBuilder builder, (ConditionalElement<ProjectPropertyGroupElement> group, ConditionalElement<ProjectPropertyElement>[] properties)[] groups)
        {
            // Handles:
            //
            //      <PropertyGroup>
            //          <Configurations>Debug;Release</Configurations>
            //          <Platforms>AnyCPU;x86</Platforms>
            //          <TargetFrameworks>net46;net47</TargetFrameworks>
            //      </PropertyGroup>

            ProjectPropertyElement? property = FindCandidateProperty(groups, builder.Definition.MultiplePropertyName);
            if (property != null)
            {
                foreach (string value in ParseValues(property.GetUnescapedValue()))
                {
                    if (IsValidGuessedDimensionValue(value))
                    {
                        builder.Source = DimensionSource.Declared;
                        builder.Value = value;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGuessImplicitDimension(GuessedDimensionValueBuilder builder, (ConditionalElement<ProjectPropertyGroupElement> group, ConditionalElement<ProjectPropertyElement>[] properties)[] groups)
        {
            // Handles:
            //
            //      <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
            //          <WarningLevel>4</WarningLevel>
            //      </PropertyGroup>
            //
            // -and-
            //
            //      <PropertyGroup>
            //          <WarningLevel Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU'>4</WarningLevel>
            //      </PropertyGroup>

            if (builder.Definition.IsVariantDimension)
                return false;

            foreach ((ConditionalElement<ProjectPropertyGroupElement> group, ConditionalElement<ProjectPropertyElement>[] properties) in groups)
            {
                if (TryGuessImplicitDimension(builder, group))
                    return true;

                foreach (ConditionalElement<ProjectPropertyElement> property in properties)
                {
                    if (TryGuessImplicitDimension(builder, property))
                        return true;
                }
            }

            return false;
        }

        private static bool TryGuessImplicitDimension<T>(GuessedDimensionValueBuilder builder, ConditionalElement<T> element)
        {
            if (element.ConditionalProperties.TryGetValue(builder.Definition.Name, out string value) && IsValidGuessedDimensionValue(value))
            {
                builder.Source = DimensionSource.Implicit;
                builder.Value = value;
                return true;
            }

            return false;
        }

        private static bool TryGuessSingularDimension(GuessedDimensionValueBuilder builder, (ConditionalElement<ProjectPropertyGroupElement>, ConditionalElement<ProjectPropertyElement>[])[] groups)
        {
            // Handles:
            //
            //  <PropertyGroup>
            //      <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
            //      <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
            //  </PropertyGroup>

            if (builder.Definition.IsVariantDimension)
                return false;

            ProjectPropertyElement? property = FindCandidateProperty(groups, builder.Definition.SingularPropertyName);
            if (property != null)
            {
                string value = property.GetUnescapedValue();
                if (IsValidGuessedDimensionValue(value))
                {
                    builder.Source = DimensionSource.Singular;
                    builder.Value = value;
                    return true;
                }
            }

            return false;
        }

        private static bool TrySolutionConfiguration(GuessedDimensionValueBuilder builder, IImmutableDictionary<string, string> solutionConfiguration)
        {
            if (!solutionConfiguration.TryGetValue(builder.Definition.Name, out string value))
                return false;

            builder.Source = DimensionSource.SolutionConfiguration;
            builder.Value = value;
            return true;
        }

        private static ProjectPropertyElement? FindCandidateProperty((ConditionalElement<ProjectPropertyGroupElement> group, ConditionalElement<ProjectPropertyElement>[] properties)[] groups, string propertyName)
        {
            // Walks the project file backwards, attempting to simulate evaluation via the construction model until 
            // it finds a candidate property, ignoring conditions that will likely evaluate to true inside VS. This
            // doesn't have to be exact, it's just a guess.

            foreach ((ConditionalElement<ProjectPropertyGroupElement> group, ConditionalElement<ProjectPropertyElement>[] properties) in groups.Reverse())
            {
                if (HasIgnorableCondition(group, propertyName))
                {
                    foreach (ConditionalElement<ProjectPropertyElement> property in properties.Reverse())
                    {
                        if (HasIgnorableCondition(property, propertyName))
                        {
                            if (StringComparers.ConfigurationDimensionNames.Equals(property.Element.Name, propertyName))
                                return property.Element;
                        }
                    }
                }
            }

            return null;
        }

        private async Task<IDimensionValuesWithDimension> BuildDimensionValuesAsync(ConfiguredProject project, Project evaluationProject, DimensionDefinition definition)
        {
            IDimensionValuesWithDimension[] values = await BuildDimensionValuesAsync(project, evaluationProject, new[] { definition });

            Assumes.True(values.Length == 0);

            return values[0];
        }

        private async Task<IDimensionValuesWithDimension[]> BuildDimensionValuesAsync(ConfiguredProject project, Project evaluationProject, IEnumerable<DimensionDefinition> dimensions)
        {
            DimensionValuesBuilder[] builders = await CreateValuesBuildersAsync(project, dimensions);

            foreach (DimensionValuesBuilder values in builders)
            {
                if (await TryFindDeclaredDimensionsAsync(values))
                    continue;

                if (TryFindImplicitDimensions(values, evaluationProject))
                    continue;

                if (await TryFindSingularDimensionAsync(values))
                    continue;

                SetDefaultValues(values);
            }

            return builders;
        }

        private static async Task<bool> TryFindDeclaredDimensionsAsync(DimensionValuesBuilder builder)
        {
            // Handles:
            //
            //      <PropertyGroup>
            //          <Configurations>Debug;Release</Configurations>
            //          <Platforms>AnyCPU;x86</Platforms>
            //          <TargetFrameworks>net46;net47</TargetFrameworks>
            //      </PropertyGroup>

            ReadOnlyCollection<string> values = (await builder.Dimension.MultipleProperty.GetValueAsStringCollectionAsync())!;

            var uniqueValues = values.Select(value => value.Trim())
                                     .Where(value => !string.IsNullOrWhiteSpace(value))
                                     .Distinct(StringComparers.ConfigurationDimensionValues)
                                     .ToList();

            if (uniqueValues.Count == 0)
                return false;

            builder.Source = DimensionSource.Declared;
            builder.Values = uniqueValues;
            return true;
        }

        private static async Task<bool> TryFindSingularDimensionAsync(DimensionValuesBuilder builder)
        {
            // Handles:
            //
            //  <PropertyGroup>
            //      <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
            //      <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
            //  </PropertyGroup>

            if (builder.Definition.IsVariantDimension)
                return false;

            string value = (await builder.Dimension.SingularProperty.GetValueAsStringAsync())!;

            value = value.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return false;

            builder.Source = DimensionSource.Singular;
            builder.Values = new[] { value };
            return true;
        }

        private static bool TryFindImplicitDimensions(DimensionValuesBuilder builder, Project project)
        {
            // Handles:
            //
            //      <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
            //          <WarningLevel>4</WarningLevel>
            //      </PropertyGroup>
            //
            // -and-
            //
            //      <PropertyGroup>
            //          <WarningLevel Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU'>4</WarningLevel>
            //      </PropertyGroup>

            if (builder.Definition.IsVariantDimension)
                return false;

            if (!project.ConditionedProperties.TryGetValue(builder.Definition.SingularPropertyName, out List<string> values))
                return false;

            var uniqueValues = values.Where(value => !string.IsNullOrWhiteSpace(value))
                                     .Distinct(StringComparers.ConfigurationDimensionValues)
                                     .ToList();
            
            if (uniqueValues.Count == 0)
                return false;

            builder.Source = DimensionSource.Implicit;
            builder.Values = uniqueValues;
            return true;
        }

        private static void SetDefaultValues(GuessedDimensionValueBuilder builder)
        {
            string? value = builder.Definition.DefaultValue;
            if (value != null)
            {
                builder.Source = DimensionSource.DefaultValue;
                builder.Value = value;
            }
        }

        private static void SetDefaultValues(DimensionValuesBuilder builder)
        {
            string? value = builder.Definition.DefaultValue;
            if (value != null)
            {
                builder.Source = DimensionSource.DefaultValue;
                builder.Values = new[] { value };
            }
        }

        private static ICollection<string> ParseValues(string values)
        {
            List<string>? uniqueValues = null;

            // We need to ensure that we return unique values in the
            // order specified in the property.
            foreach (string value in new LazyStringSplit(values, ';'))
            {
                string dimension = value.Trim();

                if (string.IsNullOrEmpty(dimension))
                    continue;

                uniqueValues ??= new List<string>();

                if (!uniqueValues.Contains(dimension, StringComparers.ConfigurationDimensionValues))
                    uniqueValues.Add(dimension);
            }

            return uniqueValues ?? (ICollection<string>)Array.Empty<string>();
        }

        private static bool IsValidGuessedDimensionValue(string value)
        {
            // If this property is derived from another property, skip it and just
            // pull default from next known values. This is better than picking a 
            // default that is not actually one of the known configs.

            return value.Length > 0 && 
                   value.IndexOf("$(", StringComparisons.ConfigurationDimensionValues) == -1;
        }

        private static Task AddDeclaredDimensionValueAsync(IDimensionValuesWithDimension value, string newValue)
        {
            ReadOnlyCollection<string> newValues = value.Values.Append(newValue)
                                                               .ToList()
                                                               .AsReadOnly();  // Must be ReadOnlyCollection

            return value.Dimension.MultipleProperty.SetValueAsync(newValues);
        }

        private Task AddImplicitDimensionValueAsync(UnconfiguredProject project, IDimensionValuesWithDimension value, string newValue)
        {
            return _projectAccessor.OpenProjectXmlForWriteAsync(project, projectXml =>
            {
                // We must create at least one use of the configuration condition. So we create a PropertyGroup with it.
                ProjectPropertyGroupElement group = projectXml.AddPropertyGroup();

                ImmutableDictionary<string, string> dimensions = EmptyDimensions.Add(value.Definition.Name, newValue);

                group.Condition = BuildUtilities.DimensionalValuePairsToCondition(dimensions);
            });
        }

        private static Task RemoveDeclaredDimensionValueAsync(IDimensionValuesWithDimension value, string oldValue)
        {
            Assumes.NotNull(value.Values);

            ReadOnlyCollection<string> newValues = value.Values.Where(v => !StringComparers.ConfigurationDimensionValues.Equals(v, oldValue))
                                                               .ToList()
                                                               .AsReadOnly();  // Must be ReadOnlyCollection

            return value.Dimension.MultipleProperty.SetValueAsync(newValues);
        }

        private static Task RenameDeclaredDimensionValueAsync(IDimensionValuesWithDimension values, string oldValue, string newValue)
        {
            Assumes.NotNull(values.Values);

            ReadOnlyCollection<string> newValues = values.Values.Select(value => StringComparers.ConfigurationDimensionValues.Equals(value, oldValue) ? newValue : value)
                                                                .ToList()
                                                                .AsReadOnly();  // Must be ReadOnlyCollection

            return values.Dimension.MultipleProperty.SetValueAsync(newValues);
        }

        private static GuessedDimensionValueBuilder[] CreateValueBuilders()
        {
            return KnownDimensions.Select(definition => new GuessedDimensionValueBuilder(definition))
                                  .ToArray();
        }

        private async Task<DimensionValuesBuilder[]> CreateValuesBuildersAsync(ConfiguredProject project, IEnumerable<DimensionDefinition> definitions)
        {
            ProjectProperties properties = GetProjectProperties(project);
            DeclaredDimensions result = await properties.GetDeclaredDimensionsPropertiesAsync();

            IRule rule = result.Rule;

            return definitions.Select(definition => new DimensionValuesBuilder(CreateDimension(rule, definition)))
                              .ToArray();
        }

        private static Dimension CreateDimension(IRule rule, DimensionDefinition definition)
        {
            var singularProperty = rule.GetProperty(definition.SingularPropertyName) as IStringProperty;
            Assumes.True(singularProperty != null, $"{DeclaredDimensions.SchemaName}.{definition.SingularPropertyName} does not exist or is not a string property");

            var multipleProperty = rule.GetProperty(definition.MultiplePropertyName) as IStringListProperty;
            Assumes.True(multipleProperty != null, $"{DeclaredDimensions.SchemaName}.{definition.MultiplePropertyName} does not exist or is not a string property");

            return new Dimension(definition, singularProperty, multipleProperty);
        }
    }
}
