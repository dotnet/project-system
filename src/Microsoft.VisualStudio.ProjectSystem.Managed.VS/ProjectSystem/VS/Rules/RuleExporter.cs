// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Rules;
using Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

#pragma warning disable 0649

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rules
{
    /// <summary>
    ///     Responsible for exporting our embedded rules so that CPS can pick them.
    /// </summary>
    internal static class RuleExporter
    {
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

            /// <summary>
            ///     Represents design-time build items containing the identities of packages known to be incompatible with Build Acceleration.
            /// </summary>
            [ExportRule(nameof(BuildAccelerationIncompatiblePackage), PropertyPageContexts.ProjectSubscriptionService)]
            [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
            [Order(Order.Default)]
            public static int BuildAccelerationIncompatiblePackageRule;
        }
    }
}
