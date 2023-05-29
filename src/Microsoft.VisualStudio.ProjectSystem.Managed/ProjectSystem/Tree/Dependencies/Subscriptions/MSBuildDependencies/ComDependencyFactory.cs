// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapabilities.ComReferences)]
internal sealed class ComDependencyFactory : MSBuildDependencyFactoryBase
{
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.ComDependency + DependencyTreeFlags.SupportsBrowse,
        unresolved: DependencyTreeFlags.ComDependency);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Com;

    public override string UnresolvedRuleName => ComReference.SchemaName;
    public override string ResolvedRuleName => ResolvedCOMReference.SchemaName;

    public override string SchemaItemType => ComReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.COM;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.COMWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.COMError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.COMPrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;

    protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(itemSpec);
    }
}
