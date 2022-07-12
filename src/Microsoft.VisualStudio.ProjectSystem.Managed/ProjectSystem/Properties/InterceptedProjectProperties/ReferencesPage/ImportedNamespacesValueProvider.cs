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
    private readonly KeyValuePairListEncoding _encoding;
    
    [ImportingConstructor]
    public ImportedNamespacesValueProvider(ConfiguredProject configuredProject, IProjectThreadingService threadingService, IProjectAccessor projectAccessor)
    {
        _configuredProject = configuredProject;
        _threadingService = threadingService;
        _projectAccessor = projectAccessor;
        _specialImports = new ConcurrentHashSet<string>();
        _encoding = new KeyValuePairListEncoding();
    }

    private async Task<ImmutableArray<(string Value, bool IsImported)>> GetSelectedImportsAsync()
    {
        ImmutableArray<(string Value, bool IsImported)> existingImports = await _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
        {
            return project
                .GetItems("Import")
                .Select(item => (Value: item.EvaluatedInclude, IsImported: item.IsImported))
                .Where(import => !string.IsNullOrEmpty(import.Value))
                .ToImmutableArray();
        });

        return existingImports;
    }
    
    private async Task<IEnumerable<(string Name, string Value)>> GetSelectedImportStringAsync()
    {
        var selectedImports = new List<(string Name, string Value)>();
        string projectName = Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath);
        bool containsProjectName = false;

        ImmutableArray<(string Value, bool IsImported)> existingImports = await GetSelectedImportsAsync();
        
        foreach ((string value, bool isImported) in existingImports)
        {
            
            if (!string.IsNullOrEmpty(value))
            {
                if (string.Equals(value, projectName))
                {
                    containsProjectName = true;
                }
                selectedImports.Add((value, isImported.ToString()));
            }
        }

        if (!containsProjectName)
        {
            selectedImports.Add((Name: projectName, Value: bool.TrueString));
        }

        return selectedImports;
    }
    
    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return _encoding.Format(await GetSelectedImportStringAsync());
    }

    public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return _encoding.Format(await GetSelectedImportStringAsync());
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        await _threadingService.SwitchToUIThread();

        var importsToAdd = _encoding.Parse(unevaluatedPropertyValue)
            .Where(pair => bool.TryParse(pair.Value, out bool _))
            .ToDictionary(pair => pair.Name, pair => bool.Parse(pair.Value));
        
        importsToAdd.Remove(Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath));

        foreach ((string Value, bool IsImported) import in await GetSelectedImportsAsync())
        {
            if (!importsToAdd.ContainsKey(import.Value))
            {
                try
                {
                    await _projectAccessor.OpenProjectForWriteAsync(_configuredProject, project =>
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem = project.GetItems("Import")
                            .First(i => string.Equals(import.Value, i.EvaluatedInclude, StringComparisons.ItemNames));

                        if (importProjectItem.IsImported)
                        {
                            _specialImports.Add(import.Value);
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
            
            importsToAdd.Remove(import.Value);
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

        return null;
    }
}
