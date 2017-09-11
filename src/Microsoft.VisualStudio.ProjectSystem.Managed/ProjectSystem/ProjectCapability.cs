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
        public const string CSharpOrVisualBasicOrFSharp = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + " | " + FSharp + ")";
        public const string VisualBasicAppDesigner = ProjectCapabilities.VB + " & " + AppDesigner;
        public const string CSharpAppDesigner = ProjectCapabilities.CSharp + " & " + AppDesigner;
        public const string FSharpAppDesigner = FSharp + " & " + AppDesigner;
        public const string CSharpOrVisualBasic = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ")";
        public const string CSharpOrVisualBasicOrFSharpLanguageService = CSharpOrVisualBasicOrFSharp + " & " + ProjectCapabilities.LanguageService;
        public const string AppDesigner = nameof(AppDesigner);
        public const string AppSettings = nameof(AppSettings);
        public const string DependenciesTree = nameof(DependenciesTree);
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
        public const string PreserveFormatting = nameof(PreserveFormatting);
        public const string ProjectConfigurationsDeclaredDimensions = ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
        public const string LanguageService = ProjectCapabilities.LanguageService;
        public const string SortByDisplayOrder = ProjectCapabilities.SortByDisplayOrder;
    }
}
