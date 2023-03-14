// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    /// <para>
    ///     Represents an operation with associated start and end events. An <see cref="ITelemetryOperation"/>
    ///     is started by calling <see cref="ITelemetryService.BeginOperation(string)"/>.
    /// </para>
    /// <para>
    ///     This type implements <see cref="IDisposable"/> to ensure that operations are completed and reported,
    ///     but consumers must also call <see cref="End(TelemetryResult)"/> to report the success or failure of
    ///     the operation.
    /// </para>
    /// </summary>
    internal interface ITelemetryOperation : IDisposable
    {
        /// <summary>
        ///     Associates the given properties with the "end" event of the operation.
        /// </summary>
        /// <param name="properties">
        ///     An <see cref="IEnumerable{T}"/> of property names and values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="properties"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="properties"/> is contains no elements.
        /// </exception>
        void SetProperties(IEnumerable<(string propertyName, object propertyValue)> properties);

        /// <summary>
        ///     Ends the operation and reports the result.
        /// </summary>
        /// <param name="result">
        ///     A <see cref="TelemetryResult"/> value indicating the result of the operation.
        /// </param>
        void End(TelemetryResult result);
    }
    
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
        ///     Begins an operation with a recorded duration. Consumers must call <see cref="ITelemetryOperation.End(TelemetryResult)"/>
        ///     on the returned <see cref="ITelemetryOperation"/> to signal the end of the operation and post the
        ///     telemetry events.
        /// </summary>
        /// <param name="eventName">
        ///     The name of the event to associate with this operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="eventName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="eventName"/> is an empty string ("").
        /// </exception>
        /// <returns>
        ///     An <see cref="ITelemetryOperation"/> representing the operation. 
        /// </returns>
        ITelemetryOperation BeginOperation(string eventName);

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        string HashValue(string value);
    }
}
