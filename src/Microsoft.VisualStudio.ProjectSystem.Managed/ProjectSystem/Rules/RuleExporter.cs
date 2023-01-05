// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;

#pragma warning disable 0649

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    /// <summary>
    ///     Responsible for exporting our embedded rules so that CPS can pick them.
    /// </summary>
    internal static class RuleExporter
    {
        private static class ProjectRules
        {
            /// <summary>
            ///     Represents the evaluation properties representing source control bindings,
            ///     typically used in projects connected to Team Foundation Source Control.
            /// </summary>
            [ExportRule(nameof(SourceControl), PropertyPageContexts.Invisible)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SourceControlRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) target frameworks
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SupportedTargetFrameworkAlias), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SupportedTargetFrameworkAliasRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) target frameworks
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SupportedTargetFramework), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SupportedTargetFrameworkRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) .NET Core target frameworks
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SupportedNETCoreAppTargetFramework), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SupportedNETCoreAppTargetFrameworkRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) .NET Framework target frameworks
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SupportedNETFrameworkTargetFramework), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SupportedNETFrameworkTargetFrameworkRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) .NET Standard target frameworks
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SupportedNETStandardTargetFramework), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SupportedNETStandardTargetFrameworkRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) target platforms
            ///     for a project.
            /// </summary>
            [ExportRule(nameof(SdkSupportedTargetPlatformIdentifier), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SdkSupportedTargetPlatformIdentifierRule;

            /// <summary>
            ///     Represents the evaluation items containing the supported (possible) target platforms
            ///     versions for a project.
            /// </summary>
            [ExportRule(nameof(SdkSupportedTargetPlatformVersion), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SdkSupportedTargetPlatformVersionRule;

            /// <summary>
            ///     Represents the evaluation properties containing the general configuration for a project.
            /// </summary>
            [ExportRule(nameof(ConfigurationGeneral), PropertyPageContexts.Project, PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int ConfigurationGeneralRule;
        }

        private static class AppDesignerRules
        {
            /// <summary>
            ///     Represents the evaluation properties that is used for AppDesigner folder services.
            /// </summary>
            [ExportRule(nameof(AppDesigner), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.AppDesigner)]
            [Order(Order.Default)]
            public static int AppDesignerRule;
        }

        /// <summary>
        ///     Contains rules for the PackageRestoreDataSource.
        /// </summary>
        private static class PackageRestoreRules
        {
            /// <summary>
            ///     Represents the design-time build items containing CLI tool references (legacy) that are passed to restore.
            /// </summary>
            [ExportRule(nameof(DotNetCliToolReference), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int DotNetCliToolReferenceRule;

            /// <summary>
            ///     Represents the design-time build items containing references to frameworks that are passed to restore.
            /// </summary>
            [ExportRule(nameof(CollectedFrameworkReference), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int CollectedFrameworkReferenceRule;

            /// <summary>
            ///     Represents the design-time build items containing packages to be downloaded that are passed to restore.
            /// </summary>
            [ExportRule(nameof(CollectedPackageDownload), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int CollectedPackageDownloadRule;

            /// <summary>
            ///     Represents the design-time build items containing the packages that the project references that are passed to restore.
            /// </summary>
            [ExportRule(nameof(CollectedPackageReference), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int CollectedPackageReferenceRule;

            /// <summary>
            ///     Represents the design-time build items containing the versions of direct and indirect package references that are passed to restore.
            /// </summary>
            [ExportRule(nameof(CollectedPackageVersion), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int CollectedPackageVersionRule;

            /// <summary>
            ///     Represents the evaluation properties that are passed that are passed to restore.
            /// </summary>
            [ExportRule(nameof(NuGetRestore), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.PackageReferences)]
            [Order(Order.Default)]
            public static int NuGetRestoreRule;
        }

        /// <summary>
        ///     Contains rules for the language service implementations (e.g. implementations of <c>IWorkspaceUpdateHandler</c>).
        /// </summary>
        private static class LanguageServiceRules
        {
            /// <summary>
            ///     Represents the design-time build items containing the compiler command-line that is passed to Roslyn.
            /// </summary>
            [ExportRule(nameof(CompilerCommandLineArgs), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNetLanguageService)]
            [Order(Order.Default)]
            public static int CompilerCommandLineArgsRule;

            /// <summary>
            ///     Represents the evaluation properties that are passed to Roslyn.
            /// </summary>
            [ExportRule(nameof(LanguageService), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNetLanguageService)]
            [Order(Order.Default)]
            public static int LanguageServiceRule;
        }

        /// <summary>
        ///     Contains rules for the <see cref="BuildUpToDateCheck"/> component.
        /// </summary>
        private static class BuildUpToDateCheckRules
        {
            /// <summary>
            ///     Represents evaluation items containing marker files indicating that reference projects have out of date references.
            /// </summary>
            [ExportRule(nameof(CopyUpToDateMarker), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int CopyUpToDateMarkerRule;

            /// <summary>
            ///     Represents the design-time build items containing resolved references path.
            /// </summary>
            [ExportRule(nameof(ResolvedCompilationReference), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int ResolvedCompilationReferencedRule;

            /// <summary>
            ///     Represents design-time build items containing the input files into the build.
            /// </summary>
            [ExportRule(nameof(UpToDateCheckInput), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckInputRule;

            /// <summary>
            ///     Represents design-time build items containing the output files of the build.
            /// </summary>
            [ExportRule(nameof(UpToDateCheckOutput), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckOutputRule;

            /// <summary>
            ///     Represents design-time build items containing a mapping between input and the output files of the build.
            /// </summary>
            [ExportRule(nameof(UpToDateCheckBuilt), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckBuiltRule;

            /// <summary>
            ///     Represents design-time build items containing items this project contributes to the output directory.
            /// </summary>
            [ExportRule(nameof(CopyToOutputDirectoryItem), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int CopyToOutputDirectoryItemRule;
        }

        /// <summary>
        ///     Contains rules for the Windows Forms designer component.
        /// </summary>
        private static class WindowsFormsConfigurationRules
        {
            [ExportRule(nameof(WindowsFormsConfiguration), PropertyPageContexts.Project)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int WindowsFormsConfigurationRule;
        }

        private static class OptionalWorkloadRules
        {
            /// <summary>
            ///     Represents the evaluation items representing optional workloads.
            /// </summary>
            [ExportRule(nameof(SuggestedWorkload), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(ProjectCapability.DotNet)]
            [Order(Order.Default)]
            public static int SuggestedWorkloadRule;
        }
    }
}
