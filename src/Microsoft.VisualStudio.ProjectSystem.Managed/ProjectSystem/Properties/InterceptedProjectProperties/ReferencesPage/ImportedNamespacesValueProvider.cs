// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider("ImportedNamespaces", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.VisualBasic)]
internal sealed class ImportedNamespacesValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly ConfiguredProject _configuredProject;
    private readonly IProjectThreadingService _threadingService;
    private readonly IProjectAccessor _projectAccessor;
    private readonly ConcurrentHashSet<string> _specialImports;
    
    [ImportingConstructor]
    public ImportedNamespacesValueProvider(ConfiguredProject configuredProject, IProjectThreadingService threadingService, IProjectAccessor projectAccessor)
    {
        _configuredProject = configuredProject;
        _threadingService = threadingService;
        _projectAccessor = projectAccessor;
        _specialImports = new ConcurrentHashSet<string>();
    }

    private async Task<ImmutableArray<(string Value, bool IsReadOnly)>> GetProjectImportsAsync()
    {
        ImmutableArray<(string Value, bool IsReadOnly)> existingImports = await _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
        {
            return project
                .GetItems("Import")
                .Select(item => (Value: item.EvaluatedInclude, IsReadOnly: item.IsImported))
                .Where(import => !string.IsNullOrEmpty(import.Value))
                .ToImmutableArray();
        });

        return existingImports;
    }

    private async Task<string> GetSelectedImportStringAsync()
    {
        return KeyValuePairListEncoding.Format(await GetSelectedImportListAsync());
    }
    
    private async Task<List<(string Import, string IsReadOnly)>> GetSelectedImportListAsync()
    {
        string projectName = Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath);

        ImmutableArray<(string Value, bool IsImported)> existingImports = await GetProjectImportsAsync();

        List<(string Import, string IsReadOnly)> selectedImports = existingImports
                .Where(pair => !string.IsNullOrEmpty(pair.Value))
                .Select(pair => (Key: pair.Value, Value: pair.IsImported.ToString())).ToList();

        bool containsProjectName = selectedImports.Any(selectedImport => string.Equals(selectedImport.Import, projectName, StringComparison.Ordinal));

        if (!containsProjectName)
        {
            selectedImports.Add((projectName, IsReadOnly: bool.TrueString));
        }

        return selectedImports;
    }
    
    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        await ((ConfiguredProject2)_configuredProject).EnsureProjectEvaluatedAsync();
        return await GetSelectedImportStringAsync();
    }

    public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return string.Join(",", (await GetSelectedImportListAsync()).Select(pair => pair.Import));
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        var importsToAdd = KeyValuePairListEncoding.Parse(unevaluatedPropertyValue)
            .Where(pair => bool.TryParse(pair.Value, out bool _))
            .ToDictionary(pair => pair.Name, pair => bool.Parse(pair.Value));

        importsToAdd.Remove(Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath));

        foreach ((string value, bool _) in await GetProjectImportsAsync())
        {
            if (!importsToAdd.ContainsKey(value))
            {
                try
                {
                    await _projectAccessor.OpenProjectForWriteAsync(_configuredProject, project =>
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem = project.GetItems("Import")
                            .First(i => string.Equals(value, i.EvaluatedInclude, StringComparisons.ItemNames));

                        if (importProjectItem.IsImported)
                        {
                            _specialImports.Add(value);
                        }
                        
                        project.RemoveItem(importProjectItem);
                    });

                }
                catch (Exception ex)
                {
                    // if an import comes from a targets file, or else if there's a race condition we can't remove it. otherwise throw
                    if (ex is not ArgumentException && ex is not InvalidOperationException)
                    {
                        throw;
                    }
                }
            }
            
            importsToAdd.Remove(value);
        }

        // Verify we have at least one valid import to add before acquiring a write lock.
        if (importsToAdd.Any(importToAdd => importToAdd.Key.Length > 0))
        {
            await _projectAccessor.OpenProjectXmlForWriteAsync(_configuredProject.UnconfiguredProject, project =>
            {
                foreach (KeyValuePair<string, bool> importToAdd in importsToAdd)
                {
                    if (importToAdd.Key.Length > 0)
                    {
                        project.AddItem("Import", importToAdd.Key);
                    }
                }
            });
        }

        return await GetSelectedImportStringAsync();
    }
}
