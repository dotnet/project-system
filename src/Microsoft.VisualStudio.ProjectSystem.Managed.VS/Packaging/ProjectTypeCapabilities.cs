// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public const string CSharp = Default + "; " + ProjectCapability.CSharp;

        /// <summary>
        ///     Represents Visual Basic's set of capabilities that are always present.
        /// </summary>
        public const string VisualBasic = Default + "; " + ProjectCapability.VisualBasic;
    }
}
