// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

[Export(typeof(IMSBuildDependencyFactory))]
// There's no "AnalyzerReferences" capability, and F# doesn't have analyzers
[AppliesTo(ProjectCapability.DependenciesTree + " & (" + ProjectCapability.CSharp + " | " + ProjectCapability.VisualBasic + ")")]
internal sealed class AnalyzerDependencyFactory : MSBuildDependencyFactoryBase
{
    // NOTE we include ProjectTreeFlags.FileSystemEntity here so that Roslyn can correctly identify the
    // analyzer's path in order to attach child nodes to these dependency items in Solution Explorer.
    // Without this flag, CPS will remove whatever file path we pass during tree construction (for
    // performance reasons).
    private static readonly DependencyFlagCache s_flagCache = new(
        resolved: DependencyTreeFlags.AnalyzerDependency + DependencyTreeFlags.SupportsBrowse + ProjectTreeFlags.FileSystemEntity,
        unresolved: DependencyTreeFlags.AnalyzerDependency + DependencyTreeFlags.SupportsBrowse + ProjectTreeFlags.FileSystemEntity);

    public override DependencyGroupType DependencyGroupType => DependencyGroupTypes.Analyzers;

    public override string UnresolvedRuleName => AnalyzerReference.SchemaName;
    public override string ResolvedRuleName => ResolvedAnalyzerReference.SchemaName;

    public override bool ResolvedItemRequiresEvaluatedItem => false;

    public override string SchemaItemType => AnalyzerReference.PrimaryDataSourceItemType;

    public override ProjectImageMoniker Icon => KnownProjectImageMonikers.CodeInformation;
    public override ProjectImageMoniker IconWarning => KnownProjectImageMonikers.CodeInformationWarning;
    public override ProjectImageMoniker IconError => KnownProjectImageMonikers.CodeInformationError;
    public override ProjectImageMoniker IconImplicit => KnownProjectImageMonikers.CodeInformationPrivate;

    public override DependencyFlagCache FlagCache => s_flagCache;

    protected internal override string GetUnresolvedCaption(string itemSpec, IImmutableDictionary<string, string> unresolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(itemSpec);
    }

    protected internal override string GetResolvedCaption(string itemSpec, string? originalItemSpec, IImmutableDictionary<string, string> resolvedProperties)
    {
        return Path.GetFileNameWithoutExtension(originalItemSpec ?? itemSpec);
    }
}
