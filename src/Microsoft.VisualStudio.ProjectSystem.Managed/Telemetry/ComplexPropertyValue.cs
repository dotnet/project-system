// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    ///     Wrapper for complex (non-scalar) property values being reported
    ///     via <see cref="ITelemetryService"/>.
    /// </summary>
    internal readonly struct ComplexPropertyValue
    {
        public object Data { get; }

        public ComplexPropertyValue(object data)
        {
            Data = data;
        }
    }
}
