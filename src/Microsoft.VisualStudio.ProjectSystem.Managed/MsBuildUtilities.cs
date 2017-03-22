// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Utitlies class to manipulate MsBuild projects.
    /// </summary>
    internal abstract class MsBuildUtilities
    {
        /// <summary>
        /// Gets a project property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Requested project property. Null if the property is not present.</returns>
        public static ProjectPropertyElement GetProperty(ProjectRootElement project, string propertyName)
        {
            Requires.NotNull(project, "project");

            return project.Properties
                .Where(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the individual values of a delimited property.
        /// </summary>
        /// <param name="propertyValue">Value of the property to evaluate.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <returns>Collection of individual values in the property.</returns>
        public static ImmutableArray<string> GetPropertyValues(string propertyValue, char delimiter = ';')
        {
            var values = propertyValue.Split(delimiter).Select(f => f.Trim());

            // We need to ensure that we return values in the specified order.
            var valuesBuilder = ImmutableArray.CreateBuilder<string>();
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    valuesBuilder.Add(value);
                }
            }

            return valuesBuilder.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
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
            StringBuilder newValue = new StringBuilder();
            foreach (var value in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                newValue.Append(value);
                newValue.Append(delimiter);
            }

            newValue.Append(valueToAppend);
            property.Value = newValue.ToString();
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

            var property = GetOrAddProperty(project, propertyName);
            StringBuilder newValue = new StringBuilder();
            foreach (string value in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                if (string.Compare(value, valueToRemove, StringComparison.Ordinal) != 0)
                {
                    newValue.Append(value);
                    newValue.Append(delimiter);
                }
            }

            property.Value = newValue.ToString().TrimEnd(delimiter);
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

            var property = GetOrAddProperty(project, propertyName);
            StringBuilder value = new StringBuilder();
            foreach (string propertyValue in GetPropertyValues(evaluatedPropertyValue, delimiter))
            {
                value.Append(string.Compare(propertyValue, oldValue, StringComparison.Ordinal) == 0 ? newValue : propertyValue);
                value.Append(delimiter);
            }

            property.Value = value.ToString().TrimEnd(delimiter);
        }

        /// <summary>
        /// Adds a property to the first property group. If there are no property groups it will create one.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Property name.</param>
        public static ProjectPropertyElement GetOrAddProperty(ProjectRootElement project, string propertyName)
        {
            Requires.NotNull(project, "project");
            var property = GetProperty(project, propertyName);

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
                    propertyGroup = project.PropertyGroups.FirstOrDefault();
                }

                return propertyGroup.AddProperty(propertyName, string.Empty);
            }
        }
    }
}
