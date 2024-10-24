// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

/// <summary>
/// Exports the set of rules required for the dependencies tree in Visual Studio.
/// </summary>
/// <remarks>
/// The dependencies tree handles this metadata a little differently to other components in the project system that export it.
/// The dependencies tree works a little differently between VS and VS Code.
/// <list type="bullet">
/// <item>In VS (which loads this assembly) the dependencies tree is loaded eagerly. We want the targets referenced by these rules to be included in the first design-time build.</item>
/// <item>In VS Code (which uses the shared layer) the tree is loaded lazily, and we don't want these targets included in the first design-time build.</item>
/// </list>
/// For this reason, we export the rules here in the VS-only layer, rather than in the shared layer (like other components).
/// </remarks>
internal static class DependenciesTreeBuildRuleRequirements
{
    [AppliesTo(AnalyzerDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedAnalyzerReference.SchemaName)]
    public static int AnalyzerReference;

    [AppliesTo(AssemblyDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedAssemblyReference.SchemaName)]
    public static int AssemblyReference;

    [AppliesTo(ComDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedCOMReference.SchemaName)]
    public static int ComReference;

    [AppliesTo(FrameworkDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedFrameworkReference.SchemaName)]
    public static int FrameworkReference;

    [AppliesTo(PackageDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedPackageReference.SchemaName)]
    public static int PackageReference;

    [AppliesTo(ProjectDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedProjectReference.SchemaName)]
    public static int ProjectReference;

    [AppliesTo(SdkDependencyFactory.AppliesTo)]
    [ExportInitialBuildRulesSubscriptions(ResolvedSdkReference.SchemaName)]
    public static int SdkReference;
}
