// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Returns the set of Startup objects (or entry point types) in a project.
    /// </summary>
    [ExportDynamicEnumValuesProvider("StartupObjectsEnumProvider")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class StartupObjectsEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Workspace _workspace;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public StartupObjectsEnumProvider([Import(typeof(VisualStudioWorkspace))] Workspace workspace, UnconfiguredProject project)
        {
            _workspace = workspace;
            _unconfiguredProject = project;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new StartupObjectsEnumGenerator(_workspace, _unconfiguredProject));
        }
    }

    internal class StartupObjectsEnumGenerator : IDynamicEnumValuesGenerator
    {
        public bool AllowCustomValues => true;
        private readonly Workspace _workspace;
        private readonly UnconfiguredProject _unconfiguredProject;

        /// <summary>
        /// When we implement WinForms support, we need to set this for VB WinForms projects
        /// </summary>
        private static bool SearchForEntryPointsInFormsOnly => false;

        [ImportingConstructor]
        public StartupObjectsEnumGenerator(Workspace workspace, UnconfiguredProject project)
        {
            _workspace = workspace;
            _unconfiguredProject = project;
        }

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            Project project = _workspace.CurrentSolution.Projects.First(p => PathHelper.IsSamePath(p.FilePath!, _unconfiguredProject.FullPath));
            Compilation? compilation = await project.GetCompilationAsync();

            IEntryPointFinderService? entryPointFinderService = project.LanguageServices.GetService<IEntryPointFinderService>();
            IEnumerable<INamedTypeSymbol>? entryPoints = entryPointFinderService?.FindEntryPoints(compilation?.GlobalNamespace, SearchForEntryPointsInFormsOnly);
            IEnumerable<string> entryPointNames = entryPoints.Select(e => e.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)));

            return entryPointNames.Select(name => (IEnumValue)new PageEnumValue(new EnumValue { Name = name, DisplayName = name })).ToArray();
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            return Task.FromResult<IEnumValue?>(value);
        }
    }
}
