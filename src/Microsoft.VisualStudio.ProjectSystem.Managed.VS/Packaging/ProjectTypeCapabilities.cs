// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Packaging
{
    internal class ProjectTypeCapabilities
    {
        /// <summary>
        ///     Represent the default set of capabilities for all .NET-based project.
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
        ///     Represents F#'s set of capabilities that are always present.
        /// </summary>
        public const string FSharp = Default + "; " +
                                     ProjectCapability.FSharp + "; " +
                                     ProjectCapability.SortByDisplayOrder + "; " +
                                     ProjectCapability.EditableDisplayOrder;

        /// <summary>
        ///     Represents C#'s set of capabilities that are always present.
        /// </summary>
        public const string CSharp = Default + "; " +
                                     ProjectCapability.CSharp + "; " + 
                                     ProjectCapabilities.SharedImports;

        /// <summary>
        ///     Represents Visual Basic's set of capabilities that are always present.
        /// </summary>
        public const string VisualBasic = Default + "; " + 
                                          ProjectCapability.VisualBasic + "; " + 
                                          ProjectCapabilities.SharedImports;
    }
}
