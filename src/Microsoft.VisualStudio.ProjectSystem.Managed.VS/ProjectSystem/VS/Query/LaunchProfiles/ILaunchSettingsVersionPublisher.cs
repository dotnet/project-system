// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Used to communicate version changes from <see cref="LaunchSettingsQueryVersionProvider"/>
    /// to <see cref="LaunchSettingsQueryVersionProviderExport"/> without establishing a
    /// dependency between the two or on Project Query API types. See <see cref="LaunchSettingsQueryVersionProvider"/>
    /// for a fuller explanation.
    /// </summary>
    internal interface ILaunchSettingsVersionPublisher
    {
        void UpdateVersions(ImmutableDictionary<string, long> versions);
    }
}
