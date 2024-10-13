// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

[Export(typeof(IMSBuildDependencyFactory))]
[AppliesTo(ProjectCapability.DependenciesTree + " & " + ProjectCapabilities.SdkReferences)]
internal class SdkDependencyFactory : MSBuildDependencyFactoryBase
{
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.SdkDependency + DependencyTreeFlags.SupportsFolderBrowse,
        unresolved: DependencyTreeFlags.SdkDependency);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Sdks;

    public override string UnresolvedRuleName => SdkReference.SchemaName;
    public override string ResolvedRuleName => ResolvedSdkReference.SchemaName;

    public override string SchemaItemType => SdkReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.SDK;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.SDKWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.SDKError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.SDKPrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;

    protected internal override string GetUnresolvedCaption(string itemSpec, IImmutableDictionary<string, string> unresolvedProperties)
    {
        return GetCaption(itemSpec, null, unresolvedProperties);
    }

    protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(originalItemSpec ?? itemSpec);
    }

    private static string GetCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> properties)
    {
        string version = properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty;

        string? baseCaption = new LazyStringSplit(itemSpec, ',').FirstOrDefault() ?? originalItemSpec ?? itemSpec;

        return string.IsNullOrEmpty(version)
            ? baseCaption
            : $"{baseCaption} ({version})";
    }

    protected internal override ProjectTreeFlags UpdateTreeFlags(string id, ProjectTreeFlags flags)
    {
        return flags.Add($"$ID:{Path.GetFileNameWithoutExtension(id)}");
    }
}
