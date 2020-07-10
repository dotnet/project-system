// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    /// <summary>
    ///     Provides common well-known <see cref="ProjectImageMoniker"/> keys.
    /// </summary>
    internal static class ProjectImageKey
    {
        /// <summary>
        ///     Represents the image key for the root of a project hierarchy.
        /// </summary>
        public const string ProjectRoot = nameof(ProjectRoot);

        /// <summary>
        ///     Represents the image key for the root of a shared project hierarchy.
        /// </summary>
        public const string SharedProjectRoot = nameof(SharedProjectRoot);

        /// <summary>
        ///     Represents the image key for the Shared.items file that is imported into this project in order to add a shared folder.
        /// </summary>
        public const string SharedItemsImportFile = nameof(SharedItemsImportFile);

        /// <summary>
        ///     Represents the image key for the AppDesigner folder (called "Properties" in C# and "My Project" in VB) when it is closed.
        /// </summary>
        public const string AppDesignerFolder = nameof(AppDesignerFolder);

        /// <summary>
        ///     Represents the image key for the AppDesigner folder (called "Properties" in C# and "My Project" in VB) when it is expanded.
        /// </summary>
        public const string ExpandedAppDesignerFolder = nameof(ExpandedAppDesignerFolder);
    }
}
