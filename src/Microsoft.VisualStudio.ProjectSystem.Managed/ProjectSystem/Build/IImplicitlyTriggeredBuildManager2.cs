// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
    internal interface IImplicitlyTriggeredBuildManager2 : IImplicitlyTriggeredBuildManager
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// An alternative to <see cref="IImplicitlyTriggeredBuildManager.OnBuildStart"/>
        /// that takes the set of startup projects associated with this implicit build, if
        /// any. Consumers should call one or the other of these methods, but not both.
        /// </summary>
        /// <remarks>
        /// Note that the set of startup projects is not always associated with an implicit
        /// build. For example, if the build is happening so that VS can run unit tests the
        /// set of startup projects isn't relevant and is unlikely to include the unit test
        /// project, anyway. In those sorts of cases it is expected that <see cref="IImplicitlyTriggeredBuildManager.OnBuildStart"/>
        /// will be used instead.
        /// </remarks>
        void OnBuildStart(ImmutableArray<string> startupProjectFullPaths);
    }
}
