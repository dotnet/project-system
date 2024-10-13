// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

// TODO also filter AppliesTo to ProjectCapabilities.ProjectReferences? same for other dependency types

[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapabilities.ProjectReferences)]
internal sealed class ProjectDependencyFactory : MSBuildDependencyFactoryBase
{
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.SupportsBrowse,
        unresolved: DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.SupportsBrowse);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Projects;

    public override string UnresolvedRuleName => ProjectReference.SchemaName;
    public override string ResolvedRuleName => ResolvedProjectReference.SchemaName;

    public override string SchemaItemType => ProjectReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.Application;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.ApplicationWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.ApplicationError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.ApplicationPrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;

    protected internal override string GetUnresolvedCaption(string itemSpec, IImmutableDictionary<string, string> unresolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(itemSpec);
    }

    protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(originalItemSpec ?? itemSpec);
    }

    protected internal override ProjectTreeFlags UpdateTreeFlags(string id, ProjectTreeFlags flags)
    {
        // Allow identification for attaching related items (from code in NuGet.Client).
        return flags.Add($"$ID:{Path.GetFileNameWithoutExtension(id)}");
    }
}
