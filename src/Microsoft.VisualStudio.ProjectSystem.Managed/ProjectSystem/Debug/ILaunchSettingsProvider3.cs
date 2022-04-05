// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///     Provides an implementation of <see cref="ILaunchSettingsProvider"/> with
    ///     additional methods for atomically updating a launch profile or global setting.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The existing <see cref="ILaunchSettingsProvider.AddOrUpdateProfileAsync"/> method
    ///     does not work well in situations where multiple update operations may run
    ///     concurrently because the retrieval of the current set of values is separated from
    ///     the later update. If operations A and B each retrieve the current profile, make
    ///     their changes, and then apply them there is a very good chance that A's changes
    ///     will overwrite B's or the other way around. The new method introduced here makes
    ///     retrieving and updating a profile an single operation.
    /// </para>
    /// <para>
    ///     The <see cref="ILaunchSettingsProvider.AddOrUpdateGlobalSettingAsync"/> method has
    ///     similar problems.
    /// </para>
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface ILaunchSettingsProvider3 : ILaunchSettingsProvider2
    {
        /// <summary>
        ///     Supports the retrieval and update of a given launch profile as a single
        ///     operation. When <paramref name="updateAction"/> is called it is given an
        ///     <see cref="IWritableLaunchProfile"/> with the current state of the
        ///     <paramref name="profileName"/> profile. It will update the profile as
        ///     appropriate and when it is done the profile is applied to the settings.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if <paramref name="profileName"/> was found and <paramref name="updateAction"/>
        ///     executed; <see langword="false"/> if <paramref name="profileName"/> was not found.
        /// </returns>
        Task<bool> TryUpdateProfileAsync(string profileName, Action<IWritableLaunchProfile> updateAction);

        /// <summary>
        ///     Supports the retrieval and update of the global settings as a single operation.
        ///     When <paramref name="updateFunction"/> is called it is given the current global
        ///     settings and is expected to return a set of the values that have changed. If the
        ///     value for a given key is <see langword="null" /> the corresponding global
        ///     setting will be removed, otherwise the setting will be updated.
        /// </summary>
        Task UpdateGlobalSettingsAsync(Func<ImmutableDictionary<string, object>, ImmutableDictionary<string, object?>> updateFunction);
    }
}
