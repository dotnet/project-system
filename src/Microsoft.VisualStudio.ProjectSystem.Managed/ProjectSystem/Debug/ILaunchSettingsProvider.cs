// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for the LaunchSettingsProvider.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface ILaunchSettingsProvider
    {
        /// <remarks>
        /// If the <see cref="ILaunchSettings"/> provided by this block are going to feed
        /// into another data flow block, strongly consider using <see cref="IVersionedLaunchSettingsProvider"/>
        /// instead as that provides a joinable variant.
        /// </remarks>
        IReceivableSourceBlock<ILaunchSettings> SourceBlock { get; }

        ILaunchSettings? CurrentSnapshot { get; }

        [Obsolete("Use ILaunchSettingsProvider2.GetLaunchSettingsFilePathAsync instead.")]
        string LaunchSettingsFile { get; }

        ILaunchProfile? ActiveProfile { get; }

        /// <summary>
        /// Replaces the current set of profiles with the contents of <paramref name="profiles"/>.
        /// If changes were made, the file will be checked out and updated. Note that the
        /// active profile in <paramref name="profiles"/> is ignored; to change the active
        /// profile use <see cref="SetActiveProfileAsync(string)"/> instead.
        /// </summary>
        Task UpdateAndSaveSettingsAsync(ILaunchSettings profiles);

        /// <summary>
        /// Blocks until at least one snapshot has been generated.
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns>
        /// The current <see cref="ILaunchSettings"/> snapshot, or <see langword="null"/> if the
        /// timeout expires before the snapshot become available.
        /// </returns>
        Task<ILaunchSettings?> WaitForFirstSnapshot(int timeout);

        /// <summary>
        /// Adds the given profile to the list and saves to disk. If a profile with the same
        /// name exists (case sensitive), it will be replaced with the new profile. If addToFront is
        /// true the profile will be the first one in the list. This is useful since quite often callers want
        /// their just added profile to be listed first in the start menu.
        /// </summary>
        Task AddOrUpdateProfileAsync(ILaunchProfile profile, bool addToFront);

        /// <summary>
        /// Removes the specified profile from the list and saves to disk.
        /// </summary>
        Task RemoveProfileAsync(string profileName);

        /// <summary>
        /// Adds or updates the global settings represented by settingName. Saves the
        /// updated settings to disk. Note that the settings object must be serializable.
        /// </summary>
        Task AddOrUpdateGlobalSettingAsync(string settingName, object settingContent);

        /// <summary>
        /// Removes the specified global setting and saves the settings to disk
        /// </summary>
        Task RemoveGlobalSettingAsync(string settingName);

        /// <summary>
        /// Sets the active profile. This just sets the property it does not validate that the setting matches an
        /// existing profile
        /// </summary>
        Task SetActiveProfileAsync(string profileName);
    }
}
