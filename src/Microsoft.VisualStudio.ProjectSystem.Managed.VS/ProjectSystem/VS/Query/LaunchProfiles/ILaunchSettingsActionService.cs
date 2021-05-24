// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Interface to separate the "business logic" of query actions related to launch
    /// profiles from the machinery needed to interact with the query types.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ILaunchSettingsActionService
    {
        /// <summary>
        /// Adds a new, empty, launch profile.
        /// </summary>
        /// <param name="commandName">
        /// The launch command to associate with the new profile.
        /// </param>
        /// <param name="newProfileName">
        /// The name to give to the new profile, or <see langword="null"/> to indicate that
        /// the service should choose a unique name.
        /// </param>
        /// <param name="cancellationToken">
        /// A token whose cancellation indicates that the caller no longer is interested in
        /// the result.
        /// </param>
        /// <returns>
        /// The new <see cref="ILaunchProfile"/>, or <see langword="null"/> if the profile
        /// could not be created.
        /// </returns>
        /// <remarks>
        /// If <paramref name="newProfileName"/> is the name of an existing launch profile,
        /// the existing profile will be replaced by the new one.
        /// </remarks>
        Task<ILaunchProfile?> AddLaunchProfileAsync(string commandName, string? newProfileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Duplicates an existing launch profile under a new name and (possibly) a
        /// different launch command.
        /// </summary>
        /// <param name="currentProfileName">
        /// The name of the existing profile to duplicate.
        /// </param>
        /// <param name="newProfileName">
        /// The name to give to the new profile, or <see langword="null"/> to indicate that
        /// the service should choose a unique name.
        /// </param>
        /// <param name="newProfileCommandName">
        /// The launch command to associate with the new profile, or <see langword="null"/>
        /// to indicate the command name should be copied from the existing profile.
        /// </param>
        /// <param name="cancellationToken">
        /// A token whose cancellation indicates that the caller no longer is interested in
        /// the result.
        /// </param>
        /// <returns>
        /// The new <see cref="ILaunchProfile"/>, or <see langword="null"/> if the profile
        /// could not be created. This may occur if the profile specified by <paramref name="currentProfileName"/>
        /// cannot be found.
        /// </returns>
        Task<ILaunchProfile?> DuplicateLaunchProfileAsync(string currentProfileName, string? newProfileName, string? newProfileCommandName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Renames an existing launch profile.
        /// </summary>
        /// <param name="currentProfileName">
        /// The name of the profile to rename.
        /// </param>
        /// <param name="newProfileName">
        /// The new name to give to the profile.
        /// </param>
        /// <param name="cancellationToken">
        /// A token whose cancellation indicates that the caller no longer is interested in
        /// the result.
        /// </param>
        /// <returns>
        /// The renamed <see cref="ILaunchProfile"/>, or <see langword="null"/> if the
        /// profile could not be renamed. This may occur if the profile specified by <paramref name="currentProfileName"/>
        /// cannot be found.
        /// </returns>
        /// <remarks>
        /// If <paramref name="newProfileName"/> is the name of an existing launch profile,
        /// the existing profile will be replaced by the newly-renamed profile.
        /// </remarks>
        Task<ILaunchProfile?> RenameLaunchProfileAsync(string currentProfileName, string newProfileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a launch profile.
        /// </summary>
        /// <param name="profileName">
        /// The name of the profile to remove.
        /// </param>
        /// <param name="cancellationToken">'
        /// A token whose cancellation indicates that the caller no longer is interested in
        /// the result.
        /// </param>
        Task RemoveLaunchProfileAsync(string profileName, CancellationToken cancellationToken = default);
    }
}
