// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;
using Microsoft.VisualStudio.Text;

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

    private async Task<ImmutableArray<string>> GetSelectedImportsAsync()
    {
        ImmutableArray<string> existingImports = await _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
        {
            return project
                .GetItems("Import")
                .Select(item => item.EvaluatedInclude)
                .Where(import => !string.IsNullOrEmpty(import))
                .ToImmutableArray();
        });

        return existingImports;
    }
    
    private async Task<string> GetSelectedImportStringAsync()
    {
        StringBuilder sb = new StringBuilder();
        string projectName = Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath);
        bool containsProjectName = false;

        ImmutableArray<string> existingImports = await GetSelectedImportsAsync();
        
        foreach (string? import in existingImports)
        {
            if (!string.IsNullOrEmpty(import))
            {
                if (string.Equals(import, projectName))
                {
                    containsProjectName = true;
                }
                sb.Append(import).Append(';');
            }
        }

        if (!containsProjectName)
        {
            sb.Append(projectName).Append(';');
        }

        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }
    
    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetSelectedImportStringAsync();
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetSelectedImportStringAsync();
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        await _threadingService.SwitchToUIThread();
        
        var importsToAdd = new HashSet<string>(new LazyStringSplit(unevaluatedPropertyValue, ';'));
        importsToAdd.Remove(Path.GetFileNameWithoutExtension(_configuredProject.UnconfiguredProject.FullPath));

        foreach (string import in await GetSelectedImportsAsync())
        {
            if (!importsToAdd.Contains(import))
            {
                try
                {
                    await _projectAccessor.OpenProjectForWriteAsync(_configuredProject, project =>
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem = project.GetItems("Import")
                            .First(i => string.Equals(import, i.EvaluatedInclude, StringComparisons.ItemNames));

                        if (importProjectItem.IsImported)
                        {
                            _specialImports.Add(import);
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
            
            importsToAdd.Remove(import);
        }

        // Verify we have at least one valid import to add before acquiring a write lock.
        if (importsToAdd.Any(importToAdd => importToAdd.Length > 0))
        {
            await _projectAccessor.OpenProjectXmlForWriteAsync(_configuredProject.UnconfiguredProject, project =>
            {
                foreach (string importToAdd in importsToAdd)
                {
                    if (importToAdd.Length > 0)
                    {
                        project.AddItem("Import", importToAdd);
                    }
                }
            });
        }

        return null;
    }
}
