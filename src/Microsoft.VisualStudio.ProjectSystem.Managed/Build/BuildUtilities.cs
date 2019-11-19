// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Build
{
    /// <summary>
    /// Utility class to manipulate MsBuild projects.
    /// </summary>
    internal static class BuildUtilities
    {
        /// <summary>
        ///     Returns a value indicating whether the specified property has a condition that 
        ///     always evaluates to <see langword="true"/>.
        /// </summary>
        public static bool HasWellKnownConditionsThatAlwaysEvaluateToTrue(ProjectPropertyElement element)
        {
            Requires.NotNull(element, nameof(element));

            // We look for known conditions that evaluate to true so that 
            // projects can have patterns where they opt in/out of target 
            // frameworks based on whether they are inside Visual Studio or not.

            // For example:
            // 
            // <TargetFrameworks>net461;net452</TargetFrameworks>
            // <TargetFrameworks Condition = "'$(BuildingInsideVisualStudio)' == 'true'">net461</TargetFrameworks>

            switch (element.Condition)
            {
                case "":
                case "true":
                case "'$(OS)' == 'Windows_NT'":
                case "'$(BuildingInsideVisualStudio)' == 'true'":
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a project property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Requested project property. Null if the property is not present.</returns>
        public static ProjectPropertyElement? GetProperty(ProjectRootElement project, string propertyName)
        {
            Requires.NotNull(project, "project");

            return project.Properties
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparisons.PropertyNames));
        }

        /// <summary>
        /// Gets the individual values of a delimited property.
        /// </summary>
        /// <param name="propertyValue">Value of the property to evaluate.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <returns>Collection of individual values in the property.</returns>
        public static IEnumerable<string> GetPropertyValues(string propertyValue, char delimiter = ';')
        {
            HashSet<string>? seen = null;

            // We need to ensure that we return values in the specified order.
            foreach (string value in new LazyStringSplit(propertyValue, delimiter))
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
        /// <param name="delimiter">Character used to delimit the property values.</param>
        public static void AppendPropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string valueToAppend, char delimiter = ';')
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var newValue = PooledStringBuilder.GetInstance();
            foreach (string value in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                newValue.Append(value);
                newValue.Append(delimiter);
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
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <remarks>
        /// If the property is not present it will be added. This means that the evaluated property
        /// value came from one of the project imports.
        /// </remarks>
        public static void RemovePropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string valueToRemove, char delimiter = ';')
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var newValue = new StringBuilder();
            bool valueFound = false;
            foreach (string value in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                if (!string.Equals(value, valueToRemove, StringComparisons.PropertyValues))
                {
                    if (newValue.Length != 0)
                    {
                        newValue.Append(delimiter);
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
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <remarks>
        /// If the property is not present it will be added. This means that the evaluated property
        /// value came from one of the project imports.
        /// </remarks>
        public static void RenamePropertyValue(ProjectRootElement project, string evaluatedPropertyValue, string propertyName, string oldValue, string newValue, char delimiter = ';')
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(evaluatedPropertyValue, nameof(evaluatedPropertyValue));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            ProjectPropertyElement property = GetOrAddProperty(project, propertyName);
            var value = new StringBuilder();
            bool valueFound = false;
            foreach (string propertyValue in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                if (value.Length != 0)
                {
                    value.Append(delimiter);
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
            Requires.NotNull(project, "project");
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

        /// <summary>
        ///     Returns a value indicating if the specified <see cref="ProjectItemInstance"/>
        ///     originated in an imported file.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="item"/> originated in an imported file;
        ///     otherwise, <see langword="false"/> if it was defined in the project being built.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsImported(this ProjectItemInstance item)
        {
            Requires.NotNull(item, nameof(item));

            string definingProjectFullPath = item.GetMetadataValue("DefiningProjectFullPath");
            string projectFullPath = item.Project.FullPath; // NOTE: This returns project being built, not owning target

            return !StringComparers.Paths.Equals(definingProjectFullPath, projectFullPath);
        }
    }
}
