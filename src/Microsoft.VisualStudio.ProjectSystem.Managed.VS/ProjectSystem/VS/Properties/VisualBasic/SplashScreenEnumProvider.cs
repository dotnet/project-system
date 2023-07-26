// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Project = Microsoft.CodeAnalysis.Project;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic;

[ExportDynamicEnumValuesProvider("SplashScreenEnumProvider")]
[AppliesTo(ProjectCapability.VisualBasic)]
internal class SplashScreenEnumProvider : IDynamicEnumValuesProvider
{
    private readonly Workspace _workspace;
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly ProjectProperties _properties;

    [ImportingConstructor]
    public SplashScreenEnumProvider([Import(typeof(VisualStudioWorkspace))] Workspace workspace,
                                    UnconfiguredProject unconfiguredProject,
                                    ProjectProperties propertiesProvider)
    {
        _workspace = workspace;
        _unconfiguredProject = unconfiguredProject;
        _properties = propertiesProvider;
    }

    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        // We only include a value representing the "not set" state if requested. This is
        // because the old property pages explicitly add the "(Not set)" value at the UI
        // layer; the new property pages do not have that option and so the value must come
        // from the enum provider.
        // When this project system no longer needs to support the old property pages we can
        // remove this and always include the "(Not set)" value.
        bool includeEmptyValue = options?.Any(pair =>
            pair.Name == "IncludeEmptyValue"
            && bool.TryParse(pair.Value, out bool optionValue)
            && optionValue) ?? false;

        return Task.FromResult<IDynamicEnumValuesGenerator>(new SplashScreenEnumGenerator(_workspace, _unconfiguredProject, _properties, includeEmptyValue, true));
    }

    internal class SplashScreenEnumGenerator : IDynamicEnumValuesGenerator
    {
        public bool AllowCustomValues => false;
        private readonly Workspace _workspace;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly ProjectProperties _properties;
        private readonly bool _includeEmptyValue;
        private readonly bool _searchForEntryPointsInFormsOnly;

        public SplashScreenEnumGenerator(Workspace workspace, UnconfiguredProject unconfiguredProject, ProjectProperties properties, bool includeEmptyValue, bool searchForEntryPointsInFormsOnly)
        {
            _workspace = workspace;
            _unconfiguredProject = unconfiguredProject;
            _properties = properties;
            _includeEmptyValue = includeEmptyValue;
            _searchForEntryPointsInFormsOnly = searchForEntryPointsInFormsOnly;
        }

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => PathHelper.IsSamePath(p.FilePath!, _unconfiguredProject.FullPath));
            if (project is null)
            {
                return Array.Empty<IEnumValue>();
            }

            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                // Project does not support compilations
                return Array.Empty<IEnumValue>();
            }

            List<IEnumValue> enumValues = new();
            if (_includeEmptyValue)
            {
                enumValues.Add(new PageEnumValue(new EnumValue { Name = string.Empty, DisplayName = VSResources.StartupObjectNotSet }));
            }

            IEntryPointFinderService? entryPointFinderService = project.Services.GetService<IEntryPointFinderService>();
            IEnumerable<INamedTypeSymbol>? entryPoints = entryPointFinderService?.FindEntryPoints(compilation.GlobalNamespace, _searchForEntryPointsInFormsOnly);
            if (entryPoints is not null)
            {
                enumValues.AddRange(entryPoints.Select(ep =>
                {
                    // These values are saved to the myapp file, where they are later used by the MyApplicationCodeGenerator to generate the Designer.vb file that contains the actual code.
                    // The code generator expects the name of the splash screen value (just the name without the namespace nor the extension) to be passed to it,
                    // so we get it from the ep.Name (i.e. SplashScreen1), and for the display name we use the fully qualified name (i.e. MyApplication.SplashScreen1.vb).
                    string displayName = ep.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return new PageEnumValue(new EnumValue { Name = ep.Name, DisplayName = displayName });
                }));
            }

            // Remove selected startup object (if any) from the list, as a user should not be able to select it again.
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            object? startupObjectObject = await configuration.StartupObject.GetValueAsync();
            
            if (startupObjectObject is string { Length: > 0 } startupObject)
            {
                enumValues.RemoveAll(ev => StringComparers.PropertyValues.Equals(ev.Name, startupObject));
            }

            return enumValues;
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            return Task.FromResult<IEnumValue?>(value);
        }
    }
}
