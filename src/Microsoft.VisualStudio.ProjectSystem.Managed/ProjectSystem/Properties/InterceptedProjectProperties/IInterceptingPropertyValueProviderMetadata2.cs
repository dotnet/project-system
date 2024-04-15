// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Metadata mapping interface for the <see cref="ExportInterceptingPropertyValueProviderAttribute"/>.
    /// </summary>
    internal interface IInterceptingPropertyValueProviderMetadata2 : IInterceptingPropertyValueProviderMetadata
    {
        /// <summary>
        /// Gets the expression that indicates where this export should be applied.
        /// </summary>
        [DefaultValue(null)]
        string? AppliesTo { get; }
    }
}
