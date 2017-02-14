// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

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
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <returns>Collection of individual values in the property.</returns>
        public static ImmutableArray<string> GetPropertyValues(ProjectRootElement project, string propertyName, char delimiter = ';')
        {
            Requires.NotNull(project, "project");
            var property = GetProperty(project, propertyName);

            if (property != null)
            {
                return GetPropertyValues(property, delimiter);
            }
            else
            {
                return ImmutableArray.Create<string>();
            }
        }

        /// <summary>
        /// Gets the individual values of a delimited property.
        /// </summary>
        /// <param name="property">Property to evaluate.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <returns>Collection of individual values in the property.</returns>
        public static ImmutableArray<string> GetPropertyValues(ProjectPropertyElement property, char delimiter = ';')
        {
            Requires.NotNull(property, "property");

            if (!string.IsNullOrEmpty(property.Value))
            {
                return GetPropertyValues(property.Value, delimiter);
            }
            else
            {
                return ImmutableArray.Create<string>();
            }
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
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        public static void AppendPropertyValue(ProjectRootElement project, string propertyName, string propertyValue, char delimiter = ';')
        {
            Requires.NotNull(project, "project");
            Requires.NotNullOrEmpty(propertyName, "propertyName");

            var property = GetProperty(project, propertyName);
            if (property != null)
            {
                // Property already exists, append value at the end
                StringBuilder newValue = new StringBuilder();
                foreach (string value in GetPropertyValues(property, delimiter))
                {
                    newValue.Append(value);
                    newValue.Append(delimiter);
                }

                newValue.Append(propertyValue);
                property.Value = newValue.ToString();
            }
            else
            {
                // No existing property, add one
                AddProperty(project, propertyName, propertyValue);
            }
        }

        /// <summary>
        /// Renames a value inside a delimited property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property value.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        public static void RemovePropertyValue(ProjectRootElement project, string propertyName, string propertyValue, char delimiter = ';')
        {
            Requires.NotNull(project, "project");
            Requires.NotNullOrEmpty(propertyName, "propertyName");

            var property = GetProperty(project, propertyName);
            if (property != null)
            {
                // Property already exists, find the value and remove it
                StringBuilder newValue = new StringBuilder();
                foreach (string value in GetPropertyValues(property, delimiter))
                {
                    if (value != propertyValue)
                    {
                        newValue.Append(value);
                        newValue.Append(delimiter);
                    }
                }

                // Write the property even if it's empty as it could be overriding a default.
                property.Value = newValue.ToString().TrimEnd(delimiter);
            }
        }

        /// <summary>
        /// Renames a value inside a delimited property.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="oldValue">Current property value.</param>
        /// <param name="newValue">New property value.</param>
        /// <param name="delimiter">Character used to delimit the property values.</param>
        /// <remarks>
        /// If the specified property is not in the project it a new one will be added with the new value.
        /// </remarks>
        public static void RenamePropertyValue(ProjectRootElement project, string propertyName, string oldValue, string newValue, char delimiter = ';')
        {
            Requires.NotNull(project, "project");
            Requires.NotNullOrEmpty(propertyName, "propertyName");

            var property = GetProperty(project, propertyName);
            if (property != null)
            {
                // Property already exists, find the value and remove it
                StringBuilder value = new StringBuilder();
                foreach (string propertyValue in GetPropertyValues(property, delimiter))
                {
                    value.Append(propertyValue == oldValue ? newValue : propertyValue);
                    value.Append(delimiter);
                }

                property.Value = value.ToString().TrimEnd(delimiter);
            }
            else
            {
                // Something went very wrong if we have a rename without the properties being
                // there, as a fallback add the new property to the project
                AddProperty(project, propertyName, newValue);
            }
        }

        /// <summary>
        /// Adds a property to the first property group. If there are no property groups it will create one.
        /// </summary>
        /// <param name="project">Xml representation of the MsBuild project.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="propertyValue">Property Value.</param>
        public static void AddProperty(ProjectRootElement project, string propertyName, string propertyValue)
        {
            Requires.NotNull(project, "project");

            ProjectPropertyGroupElement propertyGroup;
            if (project.PropertyGroups.Count == 0)
            {
                propertyGroup = project.AddPropertyGroup();
            }
            else
            {
                propertyGroup = project.PropertyGroups.FirstOrDefault();
            }

            propertyGroup.AddProperty(propertyName, propertyValue);
        }
    }
}
