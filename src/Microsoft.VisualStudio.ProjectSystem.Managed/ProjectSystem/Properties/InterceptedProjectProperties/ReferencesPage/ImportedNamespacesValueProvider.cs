// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider("ImportedNamespaces", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class ImportedNamespacesValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly Imports _imports;
    private readonly IProjectThreadingService _threadingService;

    [ImportingConstructor]
    public ImportedNamespacesValueProvider(Imports imports, IProjectThreadingService threadingService)
    {
        _imports = imports;
        _threadingService = threadingService;
    }

    private string GetSelectedImports()
    {
        StringBuilder sb = new StringBuilder();

        foreach (string? import in _imports)
        {
            if (!string.IsNullOrEmpty(import))
            {
                sb.Append($"{import};");
            }
        }

        return sb.ToString();
    }
    
    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult(GetSelectedImports());
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult(GetSelectedImports());
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        await _threadingService.SwitchToUIThread(CancellationToken.None);
        
        string[] imports = unevaluatedPropertyValue.Split(';');
        // delete existing imports that aren't in unevaluatedPropertyValue
        
        // add imports that are in unevaluatedPropertyValue but not in _imports
        var importsToAdd = imports.ToHashSet();
        foreach (string import in _imports)
        {
            if (!imports.Contains(import))
            {
                try
                {
                    _imports.Remove(import);
                }
                catch (ArgumentException) // if an import comes from a targets file, we can't remove it
                {
                }
            }
            
            importsToAdd.Remove(import);
        }

        foreach (string importToAdd in importsToAdd)
        {
            _imports.Add(importToAdd);
        }

        return null;
    }
}
