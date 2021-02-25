// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug.Designer
{
    /// <summary>
    /// Connection object provided to a launch profile editor client, via which a client
    /// may add, remove, and update profiles.
    /// </summary>
    /// <remarks>
    /// Must be disposed when the connection is no longer required.
    /// </remarks>
    internal interface ILaunchProfileEditorConnection : IAsyncDisposable
    {
        Task SetValuesAsync(Guid correlationId, ImmutableArray<PropertyValueChangeRequest> changes);

        /// <summary>
        /// Returns the set of available debug command names.
        /// </summary>
        Task<ImmutableArray<string>> GetDebugCommandNamesAsync();

        /// <summary>
        /// Adds a profile with the given name and debug command name.
        /// </summary>
        Task AddAsync(Guid correlationId, string profileName, string debugCommandName);

        /// <summary>
        /// Removes the profile with the given name.
        /// </summary>
        Task RemoveAsync(Guid correlationId, string profileName);

        /// <summary>
        /// Renames the profile with the given name.
        /// </summary>
        Task RenameAsync(Guid correlationId, string currentProfileName, string newProfileName);
    }
}
