// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry;

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
