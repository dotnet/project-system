// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="UnconfiguredProject"/>-level handler for retrieving Project Query API entities for Launch Profiles.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectLaunchProfileHandler
    {
        /// <summary>
        /// Returns entities representing all launch profiles in the project.
        /// </summary>
        Task<IEnumerable<IEntityValue>> RetrieveAllLaunchProfileEntitiesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ILaunchProfilePropertiesAvailableStatus requestedProperties);
        
        /// <summary>
        /// Returns the entity representing the launch profile specified by the given <paramref name="id"/>.
        /// </summary>
        Task<IEntityValue?> RetrieveLaunchProfileEntityAsync(IQueryExecutionContext queryExecutionContext, EntityIdentity id, ILaunchProfilePropertiesAvailableStatus requestedProperties);

        /// <summary>
        /// Adds a new, empty, launch profile.
        /// </summary>
        /// <param name="queryExecutionContext">
        /// The context of the current query execution.
        /// </param>
        /// <param name="parent">
        /// The entity representing the project.
        /// </param>
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
        /// The <see cref="EntityIdentity"/> representing the new <see cref="ILaunchProfile"/>,
        /// or <see langword="null"/> if the profile could not be created.
        /// </returns>
        /// <remarks>
        /// If <paramref name="newProfileName"/> is the name of an existing launch profile,
        /// the existing profile will be replaced by the new one.
        /// </remarks>
        Task<EntityIdentity?> AddLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string commandName, string? newProfileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Duplicates an existing launch profile under a new name and (possibly) a
        /// different launch command.
        /// </summary>
        /// <param name="queryExecutionContext">
        /// The context of the current query execution.
        /// </param>
        /// <param name="parent">
        /// The entity representing the project.
        /// </param>
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
        /// The <see cref="EntityIdentity"/> representing the new <see cref="ILaunchProfile"/>,
        /// or <see langword="null"/> if the profile could not be created. This may occur if
        /// the profile specified by <paramref name="currentProfileName"/> cannot be found.
        /// </returns>
        Task<EntityIdentity?> DuplicateLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string currentProfileName, string? newProfileName, string? newProfileCommandName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a launch profile.
        /// </summary>
        /// <param name="queryExecutionContext">
        /// The context of the current query execution.
        /// </param>
        /// <param name="parent">
        /// The entity representing the project.
        /// </param>
        /// <param name="profileName">
        /// The name of the profile to remove.
        /// </param>
        /// <param name="cancellationToken">'
        /// A token whose cancellation indicates that the caller no longer is interested in
        /// the result.
        /// </param>
        /// <returns>
        /// The <see cref="EntityIdentity"/> representing the removed <see cref="ILaunchProfile"/>,
        /// or <see langword="null"/> if the profile could not be found.
        /// </returns>
        Task<EntityIdentity?> RemoveLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string profileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Renames an existing launch profile.
        /// </summary>
        /// <param name="queryExecutionContext">
        /// The context of the current query execution.
        /// </param>
        /// <param name="parent">
        /// The entity representing the project.
        /// </param>
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
        /// A tuple consisting of two <see cref="EntityIdentity"/>s with the first
        /// representing the profile under its previous name and the second representing the
        /// profile under the new name, or <see langword="null"/> if the profile cannot be
        /// found.
        /// </returns>
        /// <remarks>
        /// If <paramref name="newProfileName"/> is the name of an existing launch profile,
        /// the existing profile will be replaced by the newly-renamed profile.
        /// </remarks>
        Task<(EntityIdentity oldProfileId, EntityIdentity newProfileId)?> RenameLaunchProfileAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, string currentProfileName, string newProfileName, CancellationToken cancellationToken = default);
    }
}
