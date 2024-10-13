// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapability.DotNet)] // There's no "FrameworkReferences" capability
internal sealed class FrameworkDependencyFactory : MSBuildDependencyFactoryBase
{
    // Framework references were introduced in .NET Core 2.0

    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.FrameworkDependency + DependencyTreeFlags.SupportsFolderBrowse,
        unresolved: DependencyTreeFlags.FrameworkDependency);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Frameworks;

    public override string UnresolvedRuleName => FrameworkReference.SchemaName;
    public override string ResolvedRuleName => ResolvedFrameworkReference.SchemaName;

    public override string SchemaItemType => FrameworkReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.Framework;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.FrameworkWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.FrameworkError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.FrameworkPrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;
}
