// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Packaging
{
    /// <summary>
    ///     Represents set of capabilities for .NET-based projects that are always present ("fixed").
    /// </summary>
    /// <remarks>
    ///     These capabilities (along with any active IProjectCapabilitiesProvider) are combined with
    ///     the "dynamic" capabilities inherited from the active configuration. These are typically
    ///     defined in Microsoft.Managed.DesignTime.targets, but could come from other locations such
    ///     as packages or other target files.
    /// </remarks>
    internal static class ProjectTypeCapabilities
    {
        /// <summary>
        ///     Represent set of capabilities for all .NET-based project that are always present ("fixed").
        /// </summary>
        public const string Default = ProjectCapability.AppDesigner + "; " +
                                      ProjectCapability.EditAndContinue + "; " +
                                      ProjectCapability.HandlesOwnReload + "; " +
                                      ProjectCapability.OpenProjectFile + "; " +
                                      ProjectCapability.PreserveFormatting + "; " +
                                      ProjectCapability.ProjectConfigurationsDeclaredDimensions + "; " +
                                      ProjectCapability.LanguageService + "; " +
                                      ProjectCapability.DotNet;

        /// <summary>
        ///     Represents F#'s (fsproj) set of capabilities that are always present ("fixed").
        /// </summary>
        public const string FSharp = Default + "; " +
                                     ProjectCapability.FSharp + "; " +
                                     ProjectCapability.SortByDisplayOrder + "; " +
                                     ProjectCapability.EditableDisplayOrder;

        /// <summary>
        ///     Represents C#'s (csproj) set of capabilities that are always present ("fixed").
        /// </summary>
        /// <remarks>
        ///     NOTE: C# Shared Project's (shproj) see a limited set of fixed capabilities defined
        ///     in CPS under codesharingproj.pkgdef.
        /// </remarks>
        public const string CSharp = Default + "; " +
                                     ProjectCapability.CSharp + "; " +
                                     ProjectCapabilities.SharedImports + "; " +
                                     ProjectCapability.UseProjectEvaluationCache;
        /// <summary>
        ///     Represents Visual Basic's (vbproj) set of capabilities that are always present ("fixed").
        /// </summary>
        /// <remarks>
        ///     NOTE: Visual Basic Shared Project's (shproj) see a limited set of fixed capabilities defined
        ///     in CPS under codesharingproj.pkgdef.
        /// </remarks>
        public const string VisualBasic = Default + "; " +
                                          ProjectCapability.VisualBasic + "; " +
                                          ProjectCapabilities.SharedImports + "; " +
                                          ProjectCapability.UseProjectEvaluationCache;
    }
}
