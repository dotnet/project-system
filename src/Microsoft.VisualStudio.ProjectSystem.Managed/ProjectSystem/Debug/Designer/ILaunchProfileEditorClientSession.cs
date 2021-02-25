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
        /// Provides an editor client with the initial snapshot of properties, including all metadata and values.
        /// </summary>
        Task ReceivePropertiesAsync(
            ImmutableArray<Property> properties);

        /// <summary>
        /// Notifies an editor client of a change to the values of one or more properties.
        /// </summary>
        /// <returns>
        /// A Task that completes when the client has received and scheduled the property update.
        /// </returns>
        Task ReceivePropertyUpdatesAsync(
            Guid? correlationId,
            ImmutableArray<PropertyUpdate> propertyUpdates);
    }
}
