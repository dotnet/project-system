// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class ProjectCapability
    {
        public const string AlwaysAvailable = ProjectCapabilities.AlwaysApplicable;
        public const string CSharp = ProjectCapabilities.CSharp;
        public const string VisualBasic = ProjectCapabilities.VB;
        public const string FSharp = nameof(FSharp);
        public const string VisualBasicAppDesigner = ProjectCapabilities.VB + " & " + AppDesigner;
        public const string CSharpAppDesigner = ProjectCapabilities.CSharp + " & " + AppDesigner;
        public const string FSharpAppDesigner = FSharp + " & " + AppDesigner;
        public const string CSharpOrVisualBasic = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ")";
        public const string CSharpOrVisualBasicLanguageService = CSharpOrVisualBasic + " & " + LanguageService;

        public const string AppDesigner = nameof(AppDesigner);
        public const string AppSettings = nameof(AppSettings);
        public const string DependenciesTree = nameof(DependenciesTree);
        public const string ProjectImportsTree = nameof(ProjectImportsTree);
        public const string EditAndContinue = nameof(EditAndContinue);
        public const string LaunchProfiles = nameof(LaunchProfiles);
        public const string OpenProjectFile = nameof(OpenProjectFile);
        public const string HandlesOwnReload = ProjectCapabilities.HandlesOwnReload;
        public const string ReferenceManagerAssemblies = nameof(ReferenceManagerAssemblies);
        public const string ReferenceManagerBrowse = nameof(ReferenceManagerBrowse);
        public const string ReferenceManagerCOM = nameof(ReferenceManagerCOM);
        public const string ReferenceManagerProjects = nameof(ReferenceManagerProjects);
        public const string ReferenceManagerSharedProjects = nameof(ReferenceManagerSharedProjects);
        public const string ReferenceManagerWinRT = nameof(ReferenceManagerWinRT);
        public const string Pack = nameof(Pack); // Keep this in sync with Microsoft.VisualStudio.Editors.ProjectCapability.Pack
        public const string PackageReferences = ProjectCapabilities.PackageReferences;
        public const string PreserveFormatting = nameof(PreserveFormatting);
        public const string ProjectConfigurationsDeclaredDimensions = ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
        public const string LanguageService = nameof(LanguageService);
        public const string DotNetLanguageService = DotNet + " & " + LanguageService;
        
        /// <summary>
        /// Instructs CPS to order tree items according to the <see cref="IProjectTree2.DisplayOrder"/> property first.
        /// This is in addition to the default ordering by <see cref="ProjectTreeFlags.Common.BubbleUp"/>, then by
        /// <see cref="ProjectTreeFlags.Common.Folder"/> or <see cref="ProjectTreeFlags.Common.VirtualFolder"/>, and finally
        /// alphabetical.
        /// </summary>
        public const string SortByDisplayOrder = ProjectCapabilities.SortByDisplayOrder;
        
        /// <summary>
        /// Enables commands and behaviour that allows reordering items in the tree.
        /// Used by F# projects, for which item order is significant to compilation.
        /// </summary>
        public const string EditableDisplayOrder = nameof(EditableDisplayOrder);
        
        public const string DotNet = ".NET";
        public const string WindowsForms = nameof(WindowsForms);
        public const string WPF = nameof(WPF);
    }
}
