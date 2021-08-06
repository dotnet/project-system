// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Build
{
    /// <summary>
    /// Utility class to manipulate MsBuild projects.
    /// </summary>
    internal static class BuildUtilities
    {
        private const char ListDelimiter = ';';

        /// <summary>
        ///     Returns a value indicating whether the specified property has a condition that
        ///     always evaluates to <see langword="true"/>.
        /// </summary>
        public static bool HasWellKnownConditionsThatAlwaysEvaluateToTrue(ProjectPropertyElement element)
        {
            return HasWellKnownConditionsThatAlwaysEvaluateToTrue(element.Condition);
        }

        public static bool HasWellKnownConditionsThatAlwaysEvaluateToTrue(string condition)
        {
            Requires.NotNull(condition, nameof(condition));

            // We look for known conditions that evaluate to true so that 
            // projects can have patterns where they opt in/out of target 
            // frameworks based on whether they are inside Visual Studio or not.

            // For example:
            // 
            // <TargetFrameworks>net461;net452</TargetFrameworks>
            // <TargetFrameworks Condition = "'$(BuildingInsideVisualStudio)' == 'true'">net461</TargetFrameworks>

            return condition switch
            {
                "" => true,
                "true" => true,
                "'$(OS)' == 'Windows_NT'" => true,
                "'$(BuildingInsideVisualStudio)' == 'true'" => true,
                _ => false,
            };
        }

        /// <summary>
        /// Gets a project property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Requested project property. Null if the property is not present.</returns>
        public static ProjectPropertyElement? GetProperty(ProjectRootElement project, string propertyName)
        {
            Requires.NotNull(project, nameof(project));

            return project.Properties
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparisons.PropertyNames));
        }

        /// <summary>
        /// Gets the individual values of a delimited property.
        /// </summary>
        /// <param name="propertyValue">Value of the property to evaluate.</param>
        /// <returns>Collection of individual values in the property.</returns>
        public static IEnumerable<string> GetPropertyValues(string propertyValue)
        {
            HashSet<string>? seen = null;

            // We need to ensure that we return values in the specified order.
            foreach (string value in new LazyStringSplit(propertyValue, ListDelimiter))
            {
                string s = value.Trim();

                if (!string.IsNullOrEmpty(s))
                {
                    if (seen == null)
                    {
                        seen = new HashSet<string>(StringComparers.PropertyValues) { s };
                        yield return s;
                    }
                    else
                    {
                        if (seen.Add(s))
                        {
                            yield return s;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Appends a value to a delimited property. If the property does not exist it will be added.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="evaluatedPropertyValue">Original evaluated value of the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="valueToAppend">Value to add to the property.</param>
        public static void AppendPropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string valueToAppend)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var newValue = PooledStringBuilder.GetInstance();
            foreach (string value in GetPropertyValues(evaluatedPropertyValue))
            {
                newValue.Append(value);
                newValue.Append(ListDelimiter);
            }

            newValue.Append(valueToAppend);
            property.Value = newValue.ToStringAndFree();
        }

        /// <summary>
        /// Renames a value inside a delimited property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="evaluatedPropertyValue">Original evaluated value of the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="valueToRemove">Value to remove from the property.</param>
        /// <remarks>
        /// If the property is not present it will be added. This means that the evaluated property
        /// value came from one of the project imports.
        /// </remarks>
        public static void RemovePropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string valueToRemove)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var newValue = new StringBuilder();
            bool valueFound = false;
            foreach (string value in GetPropertyValues(evaluatedPropertyValue))
            {
                if (!string.Equals(value, valueToRemove, StringComparisons.PropertyValues))
                {
                    if (newValue.Length != 0)
                    {
                        newValue.Append(ListDelimiter);
                    }

                    newValue.Append(value);
                }
                else
                {
                    valueFound = true;
                }
            }

            property.Value = newValue.ToString();

            if (!valueFound)
            {
                throw new ArgumentException(string.Format(Resources.MsBuildMissingValueToRemove, valueToRemove, propertyName), nameof(valueToRemove));
            }
        }

        /// <summary>
        /// Renames a value inside a delimited property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="evaluatedPropertyValue">Original evaluated value of the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="oldValue">Current property value.</param>
        /// <param name="newValue">New property value.</param>
        /// <remarks>
        /// If the property is not present it will be added. This means that the evaluated property
        /// value came from one of the project imports.
        /// </remarks>
        public static void RenamePropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string? oldValue, string newValue)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var value = new StringBuilder();
            bool valueFound = false;
            foreach (string propertyValue in GetPropertyValues(evaluatedPropertyValue))
            {
                if (value.Length != 0)
                {
                    value.Append(ListDelimiter);
                }

                if (string.Equals(propertyValue, oldValue, StringComparisons.PropertyValues))
                {
                    value.Append(newValue);
                    valueFound = true;
                }
                else
                {
                    value.Append(propertyValue);
                }
            }

            property.Value = value.ToString();

            if (!valueFound)
            {
                throw new ArgumentException(string.Format(Resources.MsBuildMissingValueToRename, oldValue, propertyName), nameof(oldValue));
            }
        }

        /// <summary>
        /// Adds a property to the first property group. If there are no property groups it will create one.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Property name.</param>
        public static ProjectPropertyElement GetOrAddProperty(ProjectRootElement project, string propertyName)
        {
            Requires.NotNull(project, nameof(project));
            ProjectPropertyElement? property = GetProperty(project, propertyName);

            if (property != null)
            {
                return property;
            }
            else
            {
                ProjectPropertyGroupElement propertyGroup;
                if (project.PropertyGroups.Count == 0)
                {
                    propertyGroup = project.AddPropertyGroup();
                }
                else
                {
                    propertyGroup = project.PropertyGroups.First();
                }

                return propertyGroup.AddProperty(propertyName, string.Empty);
            }
        }

        private static readonly Regex s_dimensionNameInConditionRegex = new(@"^\$\(([^\$\(\)]*)\)$");

        /// <summary>
        /// Converts configuration dimensional value vector to a msbuild condition
        /// Use the standard format of
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="dimensionalValues">vector of configuration dimensional properties</param>
        /// <returns>msbuild condition representation</returns>
        internal static string DimensionalValuePairsToCondition(IReadOnlyDictionary<string, string> dimensionalValues)
        {
            if (dimensionalValues.Count == 0)
            {
                return string.Empty; // no condition. Returns empty string to match MSBuild.
            }

            string left = string.Empty;
            string right = string.Empty;

            foreach (string key in dimensionalValues.Keys)
            {
                if (!string.IsNullOrEmpty(left))
                {
                    left += "|";
                    right += "|";
                }

                left += "$(" + key + ")";
                right += dimensionalValues[key];
            }

            string condition = "'" + left + "'=='" + right + "'";
            return condition;
        }

        /// <summary>
        /// Opposite to DimensionalValuePairsToCondition. Tries to parse an MSBuild condition to a dimensional vector
        /// only matches standard pattern:
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="condition">msbuild condition string</param>
        /// <param name="dimensionalValues">configuration dimensions vector (output)</param>
        /// <returns>true on success</returns>
        internal static bool TryCalculateConditionalProperties(string condition, [NotNullWhen(returnValue: true)]out IReadOnlyDictionary<string, string>? dimensionalValues)
        {
            Requires.NotNull(condition, nameof(condition));

            dimensionalValues = null;

            int equalIndex = condition.IndexOf("==", StringComparison.OrdinalIgnoreCase);
            if (equalIndex <= 0)
            {
                return false;
            }

            string left = condition.Substring(0, equalIndex).Trim();
            string right = condition.Substring(equalIndex + 2).Trim();

            // left and right needs to ba a valid quoted strings
            if (!UnquoteString(ref left) || !UnquoteString(ref right))
            {
                return false;
            }

            string[] dimensionNamesInCondition = left.Split(Delimiter.Pipe);
            string[] dimensionValuesInCondition = right.Split(Delimiter.Pipe);

            // number of keys need to match number of values
            if (dimensionNamesInCondition.Length != dimensionValuesInCondition.Length)
            {
                return false;
            }

            var parsedDimensionalValues = new Dictionary<string, string>(dimensionNamesInCondition.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dimensionNamesInCondition.Length; i++)
            {
                // Matches "$(Configuration)" pattern.
                Match match = s_dimensionNameInConditionRegex.Match(dimensionNamesInCondition[i]);
                if (!match.Success)
                {
                    return false;
                }

                // Pull out "Configuration" in "$(Configuration)"
                string dimensionName = match.Groups[1].ToString();
                if (dimensionName.Length == 0)
                {
                    return false;
                }

                parsedDimensionalValues[dimensionName] = dimensionValuesInCondition[i];
            }

            dimensionalValues = parsedDimensionalValues;
            return true;
        }

        private static bool UnquoteString(ref string s)
        {
            if (s.Length < 2 || s[0] != '\'' || s[s.Length - 1] != '\'')
            {
                return false;
            }

            s = s.Substring(1, s.Length - 2);
            return true;
        }
    }
}
