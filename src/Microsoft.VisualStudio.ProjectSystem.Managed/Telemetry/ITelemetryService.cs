// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITelemetryService
    {
        /// <summary>
        ///     Posts an event with the specified event name.
        /// </summary>
        /// <param name="eventName">
        ///     The name of the event.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="eventName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="eventName"/> is an empty string ("").
        /// </exception>
        void PostEvent(string eventName);

        /// <summary>
        ///     Posts an event with the specified event name and property with the
        ///     specified name and value.
        /// </summary>
        /// <param name="eventName">
        ///     The name of the event.
        /// </param>
        /// <param name="propertyName">
        ///     The name of the property.
        /// </param>
        /// <param name="propertyValue">
        ///     The value of the property.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="eventName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="propertyName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="propertyValue"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="eventName"/> is an empty string ("").
        ///     <para>
        ///         -or
        ///     </para>
        ///     <paramref name="propertyName"/> is an empty string ("").
        /// </exception>
        void PostProperty(string eventName, string propertyName, object propertyValue);

        /// <summary>
        ///     Posts an event with the specified event name and properties with the
        ///     specified names and values.
        /// </summary>
        /// <param name="eventName">
        ///     The name of the event.
        /// </param>
        /// <param name="properties">
        ///     An <see cref="IEnumerable{T}"/> of property names and values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="eventName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="properties"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="eventName"/> is an empty string ("").
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="properties"/> is contains no elements.
        /// </exception>
        void PostProperties(string eventName, IEnumerable<(string propertyName, object propertyValue)> properties);

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        string HashValue(string value);
    }
}
