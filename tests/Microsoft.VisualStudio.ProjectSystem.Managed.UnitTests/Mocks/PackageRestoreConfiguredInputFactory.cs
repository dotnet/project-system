// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static class PackageRestoreConfiguredInputFactory
    {
        public static IReadOnlyCollection<PackageRestoreConfiguredInput> Create(ProjectRestoreInfo restoreInfo)
        {
            ProjectConfiguration projectConfiguration = ProjectConfigurationFactory.Create("Debug|x64");
            IComparable projectVersion = 0;

            return new[] { new PackageRestoreConfiguredInput(projectConfiguration, restoreInfo, projectVersion) };
        }
    }
}
