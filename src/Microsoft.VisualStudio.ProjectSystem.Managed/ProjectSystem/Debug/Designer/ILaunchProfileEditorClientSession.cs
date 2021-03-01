// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug.Designer
{
    /// <summary>
    /// Proffered by the client, for notifications from the server.
    /// </summary>
    internal interface ILaunchProfileEditorClientSession
    {
        /// <summary>
        /// <para>
        /// Informs the client editor that a profile has been added, including all metadata and values.
        /// </para>
        /// <para>
        /// When the client first connects to the server this method will be called multiple times to inform the client of all existing profiles.
        /// </para>
        /// </summary>
        /// <param name="correlationId">
        /// The correlation ID provided by the client, if this profile is being added as a result of calling <see cref="ILaunchProfileEditorConnection.AddAsync(Guid, string, string)"/>.
        /// </param>
        /// <param name="profile">The newly-added <see cref="Profile"/> along with all metadata and properties.</param>
        /// <returns>
        /// A Task that completes when the client has received and scheduled the profile addition.
        /// </returns>
        Task LaunchProfileAddedAsync(Guid? correlationId, Profile profile);

        /// <summary>
        /// Informs the client editor that a profile has been removed.
        /// </summary>
        /// <param name="correlationId">
        /// The correlation ID provided by the client, if the profile is being removed as a result of calling <see cref="ILaunchProfileEditorConnection.RemoveAsync(Guid, string)"/>.
        /// </param>
        /// <param name="profileName">
        /// The name of the removed profile.
        /// </param>
        /// <returns>
        /// A Task that completes when the client has received and scheduled the profile removal.
        /// </returns>
        Task LaunchProfileRemovedAsync(Guid? correlationId, string profileName);

        /// <summary>
        /// Informs the client editor that a profile has been renamed.
        /// </summary>
        /// <param name="correlationId">
        /// The correlation ID provided by the client, if the profile is being renamed as a result of calling <see cref="ILaunchProfileEditorConnection.RenameAsync(Guid, string, string)"/>.
        /// </param>
        /// <param name="oldProfileName">
        /// The old profile name.
        /// </param>
        /// <param name="newProfileName">
        /// The new profile name.
        /// </param>
        /// <returns>
        /// A Task that completes when the client has received and scheduled the profile rename.
        /// </returns>
        Task LaunchProfileRenamedAsync(Guid? correlationId, string oldProfileName, string newProfileName);

        /// <summary>
        /// Notifies an editor client of a change to the values of one or more properties in a profile.
        /// </summary>
        /// <param name="correlationId">
        /// The correlation ID provided by the client, if the properties are being updated as a result of calling <see cref="ILaunchProfileEditorConnection.SetValuesAsync(Guid, string, ImmutableArray{PropertyValueChangeRequest})"/>.
        /// </param>
        /// <param name="profileName">
        /// The name of the updated profile.
        /// </param>
        /// <param name="propertyUpdates">
        /// The set of updated properties and their new values.
        /// </param>
        /// <returns>
        /// A Task that completes when the client has received and scheduled the property update.
        /// </returns>
        Task ReceivePropertyUpdatesAsync(
            Guid? correlationId,
            string profileName,
            ImmutableArray<PropertyUpdate> propertyUpdates);
    }
}
