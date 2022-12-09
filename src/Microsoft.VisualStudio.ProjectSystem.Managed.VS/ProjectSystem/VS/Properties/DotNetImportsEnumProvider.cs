// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportDynamicEnumValuesProvider("DotNetImportsEnumProvider")]
[AppliesTo(ProjectCapability.VisualBasic)]
internal class DotNetImportsEnumProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
{
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly Workspace _workspace;

    [ImportingConstructor]
    public DotNetImportsEnumProvider(
        UnconfiguredProject unconfiguredProject,
        [Import(typeof(VisualStudioWorkspace))]
        Workspace workspace)
    {
        _unconfiguredProject = unconfiguredProject;
        _workspace = workspace;
    }

    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(this);
    }

    private async Task<IEnumerable<string>> GetNamespacesAsync()
    {
        Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(
            proj => StringComparers.Paths.Equals(proj.FilePath, _unconfiguredProject.FullPath));

        Compilation? compilation = project is null ? null : await project.GetCompilationAsync();

        if (compilation is null)
        {
            return Enumerable.Empty<string>();
        }

        List<string> namespaceNames = new List<string>();
        Stack<INamespaceSymbol> namespacesToProcess = new Stack<INamespaceSymbol>();
        namespacesToProcess.Push(compilation.GlobalNamespace);

        while (namespacesToProcess.Count != 0)
        {
            foreach (INamespaceSymbol childNamespace in namespacesToProcess.Pop().GetNamespaceMembers())
            {
                if (NamespaceIsReferenceableFromCompilation(childNamespace, compilation))
                {
                    namespaceNames.Add(childNamespace.ToDisplayString());
                }

                namespacesToProcess.Push(childNamespace);
            }
        }

        static bool NamespaceIsReferenceableFromCompilation(INamespaceSymbol namespaceSymbol, Compilation compilation)
        {
            foreach (INamedTypeSymbol typeMember in namespaceSymbol.GetTypeMembers())
            {
                if (typeMember.CanBeReferencedByName
                    && (typeMember.DeclaredAccessibility == Accessibility.Public
                        || SymbolEqualityComparer.Default.Equals(typeMember.ContainingAssembly, compilation.Assembly)
                        || typeMember.ContainingAssembly.GivesAccessTo(compilation.Assembly)))
                {
                    return true;
                }
            }

            return false;
        }

        return namespaceNames;
    }

    public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
    {
        return (await GetNamespacesAsync()).Select(namespaceString => (IEnumValue)new PageEnumValue(new EnumValue
        {
            Name = namespaceString, DisplayName = namespaceString
        })).ToImmutableList();
    }

    public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
    {
        var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
        return Task.FromResult<IEnumValue?>(value);
    }

    public bool AllowCustomValues => true;
}
