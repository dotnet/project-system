// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    internal static class DependencyServices
    {
        /// <summary>
        ///     Returns the "BrowsePath" property from the browse object, which represents the path
        ///     to the file on disk, or in the case of framework, package and SDK references
        ///     the containing folder.
        /// </summary>
        public static async Task<string?> GetBrowsePathAsync(UnconfiguredProject project, IProjectTree node)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(node, nameof(node));

            string? path = await GetMaybeRelativeBrowsePathAsync(project, node);
            if (path is null)
                return null;

            return project.MakeRooted(path);
        }

        private static async Task<string?> GetMaybeRelativeBrowsePathAsync(UnconfiguredProject project, IProjectTree node)
        {
            Assumes.True(node.Flags.Contains(DependencyTreeFlags.Dependency));

            // Shared Projects are special, the file path points directly to the import
            if (node.Flags.Contains(DependencyTreeFlags.SharedProjectDependency))
            {
                return await GetSharedAssetsProjectAsync(project, node);
            }

            if (node.BrowseObjectProperties is null)
                return null;

            // This property typically only exists for "Resolved" dependencies with exception 
            // to analyzers/projects which can provide these paths at evaluation time.
            string path = await node.BrowseObjectProperties.GetPropertyValueAsync(ResolvedAssemblyReference.BrowsePathProperty);

            return path.Length == 0 ? null : path;
        }

        private static async Task<string?> GetSharedAssetsProjectAsync(UnconfiguredProject project, IProjectTree node)
        {
            Assumes.NotNull(node.FilePath);

            // Map from [project].projitems -> [project].shproj
            ISharedProjectFileRegistrationService sharedProjectFileRegistrationService = project.Services.ExportProvider.GetExportedValue<ISharedProjectFileRegistrationService>();
            IProjectThreadingService threadingService = project.Services.ExportProvider.GetExportedValue<IProjectThreadingService>();

            await threadingService.SwitchToUIThread();

            return sharedProjectFileRegistrationService.GetOwnerProjectForSharedProjectFile(project.MakeRooted(node.FilePath));
        }
    }
}
