// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(ProjectCapability.DependenciesTree + " & (" + ProjectCapabilities.AssemblyReferences + " | " + ProjectCapabilities.WinRTReferences + ")")]
internal sealed class AssemblyDependencyFactory : MSBuildDependencyFactoryBase
{
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.AssemblyDependency + DependencyTreeFlags.SupportsBrowse,
        unresolved: DependencyTreeFlags.AssemblyDependency);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Assemblies;

    public override string UnresolvedRuleName => AssemblyReference.SchemaName;
    public override string ResolvedRuleName => ResolvedAssemblyReference.SchemaName;

    public override string SchemaItemType => AssemblyReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.Reference;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.ReferenceWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.ReferenceError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.ReferencePrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;

    protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        string? fusionName = resolvedProperties?.GetStringProperty(ResolvedAssemblyReference.FusionNameProperty);

        // TODO How expensive is AssemblyName.ctor? Create a cache of these values?
        return fusionName is null
            ? itemSpec
            : new AssemblyName(fusionName).Name;
    }
}
