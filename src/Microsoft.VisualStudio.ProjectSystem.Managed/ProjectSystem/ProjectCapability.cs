// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class ProjectCapability
    {
        public const string CSharp = ProjectCapabilities.CSharp;
        public const string VisualBasic = ProjectCapabilities.VB;
        public const string FSharp = nameof(FSharp);
        public const string VisualBasicAppDesigner = ProjectCapabilities.VB + " & " + AppDesigner;
        public const string CSharpAppDesigner = ProjectCapabilities.CSharp + " & " + AppDesigner;
        public const string FSharpAppDesigner = FSharp + " & " + AppDesigner;
        public const string CSharpOrFSharp = "(" + CSharp + " | " + FSharp + ")";
        public const string CSharpOrVisualBasic = "(" + ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB + ")";
        public const string CSharpOrVisualBasicLanguageService = CSharpOrVisualBasic + " & " + LanguageService;
        public const string RazorAndEitherWinFormsOrWpf = "(" + DotNetRazor + "|" + "(" + WindowsForms + "&" + WPF + ")" + ")";

        public const string AppDesigner = nameof(AppDesigner);
        public const string AppSettings = nameof(AppSettings);
        public const string DependenciesTree = nameof(DependenciesTree);
        public const string ProjectImportsTree = nameof(ProjectImportsTree);
        public const string EditAndContinue = nameof(EditAndContinue);
        public const string LaunchProfiles = nameof(LaunchProfiles);
        public const string OpenProjectFile = nameof(OpenProjectFile);
        public const string HandlesOwnReload = ProjectCapabilities.HandlesOwnReload;
        public const string Pack = nameof(Pack); // Keep this in sync with Microsoft.VisualStudio.Editors.ProjectCapability.Pack
        public const string PackageReferences = ProjectCapabilities.PackageReferences;
        public const string PreserveFormatting = nameof(PreserveFormatting);
        public const string ProjectConfigurationsDeclaredDimensions = ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
        public const string LanguageService = nameof(LanguageService);
        public const string DotNetLanguageService = DotNet + " & " + LanguageService;
        public const string UseProjectEvaluationCache = ProjectCapabilities.UseProjectEvaluationCache;
        public const string SingleTargetBuildForStartupProjects = nameof(SingleTargetBuildForStartupProjects);
        public const string ProjectPropertyInterception = nameof(ProjectPropertyInterception);
        public const string WindowsForms = nameof(WindowsForms);
        public const string WPF = nameof(WPF);
        public const string SupportUniversalAuthentication = nameof(SupportUniversalAuthentication);

        public const string DotNet = ".NET";
        public const string DotNetRazor = "DotNetCoreRazor";

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
    }
}
