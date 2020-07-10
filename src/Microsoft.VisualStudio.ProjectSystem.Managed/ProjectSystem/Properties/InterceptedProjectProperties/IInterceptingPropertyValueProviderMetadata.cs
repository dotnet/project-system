// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Metadata mapping interface for the <see cref="ExportInterceptingPropertyValueProviderAttribute"/>.
    /// </summary>
    public interface IInterceptingPropertyValueProviderMetadata
    {
#pragma warning disable CA1819 // Properties should not return arrays

        /// <summary>
        /// Property names handled by the provider.
        /// This must match <see cref="ExportInterceptingPropertyValueProviderAttribute.PropertyNames" />.
        /// </summary>
        string[] PropertyNames { get; }

#pragma warning restore CA1819 // Properties should not return arrays
    }
}
