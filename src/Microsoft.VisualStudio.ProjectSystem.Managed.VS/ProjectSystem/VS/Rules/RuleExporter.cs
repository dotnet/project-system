// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

#pragma warning disable 0649

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rules
{
    /// <summary>
    ///     Responsible for exporting our embedded rules so that CPS can pick them.
    /// </summary>
    internal static class VSRuleExporter
    {
        /// <summary>
        ///     Contains rules for the <see cref="BuildUpToDateCheck"/> component.
        /// </summary>
        private static class BuildUpToDateCheckRules
        {
            /// <summary>
            ///     Represents evaluation items containing marker files indicating that reference projects have out of date references.
            /// </summary>
            [ExportVSRule(nameof(CopyUpToDateMarker), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int CopyUpToDateMarkerRule;

            /// <summary>
            ///     Represents the design-time build items containing resolved references path.
            /// </summary>
            [ExportVSRule(nameof(ResolvedCompilationReference), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(ResolvedCompilationReference))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int ResolvedCompilationReferencedRule;

            /// <summary>
            ///     Represents design-time build items containing the input files into the build.
            /// </summary>
            [ExportVSRule(nameof(UpToDateCheckInput), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(UpToDateCheckInput))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckInputRule;

            /// <summary>
            ///     Represents design-time build items containing the output files of the build.
            /// </summary>
            [ExportVSRule(nameof(UpToDateCheckOutput), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(UpToDateCheckOutput))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckOutputRule;

            /// <summary>
            ///     Represents design-time build items containing a mapping between input and the output files of the build.
            /// </summary>
            [ExportVSRule(nameof(UpToDateCheckBuilt), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(UpToDateCheckBuilt))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int UpToDateCheckBuiltRule;

            /// <summary>
            ///     Represents design-time build items containing items this project contributes to the output directory.
            /// </summary>
            [ExportVSRule(nameof(CopyToOutputDirectoryItem), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(CopyToOutputDirectoryItem))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int CopyToOutputDirectoryItemRule;

            /// <summary>
            ///     Represents design-time build items containing the identities of packages known to be incompatible with Build Acceleration.
            /// </summary>
            [ExportVSRule(nameof(BuildAccelerationIncompatiblePackage), PropertyPageContexts.ProjectSubscriptionService)]
            [ExportDesignTimeBuildTargets(nameof(BuildAccelerationIncompatiblePackage))]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int BuildAccelerationIncompatiblePackageRule;

            /// <summary>
            ///     Represents the design-time build items containing project references.
            /// </summary>
            [ExportDesignTimeBuildTargets(ResolvedProjectReference.SchemaName)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int ResolvedProjectReferenceRule;

            /// <summary>
            ///     Represents the design-time build items containing analyzer references.
            /// </summary>
            [ExportDesignTimeBuildTargets(ResolvedAnalyzerReference.SchemaName)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int ResolvedAnalyzerReferenceRule;
        }
    }
}
