// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

/// <summary>
/// Exports the set of rules required for the dependencies tree in Visual Studio.
/// </summary>
/// <remarks>
/// We handle this metadata a little differently for the dependencies tree. We want different behavior between VS and VS Code.
/// <list type="bullet">
/// <item>In VS (which loads this assembly) the dependencies tree is loaded eagerly. We want the targets referenced by these rules to be included in the first design-time build.</item>
/// <item>In VS Code (which uses the shared layer) the tree is loaded lazily, and we don't want these targets included in the first design-time build.</item>
/// </list>
/// </remarks>
[ExportInitialBuildRulesSubscriptions([
    ResolvedAnalyzerReference.SchemaName,
    ResolvedAssemblyReference.SchemaName,
    ResolvedCOMReference.SchemaName,
    ResolvedFrameworkReference.SchemaName,
    ResolvedPackageReference.SchemaName,
    ResolvedProjectReference.SchemaName,
    ResolvedSdkReference.SchemaName
    ])]
internal sealed class DependenciesTreeBuildRuleRequirements
{
    private DependenciesTreeBuildRuleRequirements()
    {
        // Not intended for instantiation. This class exists to provide metadata via MEF only.
    }
}
