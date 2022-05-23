// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    ///     Provides methods for reading values from the registry.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRegistry
    {
        /// <summary>
        /// Opens a registry key denoted by <paramref name="keyPath"/> for the current user,
        /// and returns the value (if any) associated with <paramref name="name"/>. Returns
        /// <see langword="null"/> if the registry key or value do not exist, or if an error
        /// occurs while reading the value.
        /// </summary>
        object? ReadValueForCurrentUser(string keyPath, string name);
    }
}

