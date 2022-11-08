// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider("ImplicitUsingsEditor", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.CSharp)]
internal class ImplicitUsingsValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly IProjectAccessor _projectAccessor;
    private readonly ConfiguredProject _configuredProject;
    private readonly IProjectThreadingService _threadingService;

    private const string UsingItemType = "Using";

    [ImportingConstructor]
    public ImplicitUsingsValueProvider(IProjectAccessor projectAccessor, ConfiguredProject configuredProject, IProjectThreadingService threadingService)
    {
        _projectAccessor = projectAccessor;
        _configuredProject = configuredProject;
        _threadingService = threadingService;
    }

    private async Task<List<ImplicitUsing>> GetImplicitUsingsAsync()
    {
        await ((ConfiguredProject2)_configuredProject).EnsureProjectEvaluatedAsync();

        List<ImplicitUsing> usings = await _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
        {
            return project
                .GetItems(UsingItemType)
                .Select(item =>
                {
                    string? isStaticMetadata = item.DirectMetadata.FirstOrDefault(metadata => metadata.Name.Equals("Static", StringComparisons.PropertyValues))?.EvaluatedValue;
                    string? aliasMetadata = item.DirectMetadata.FirstOrDefault(metadata => metadata.Name.Equals("Alias", StringComparisons.PropertyValues))?.EvaluatedValue;

                    return new ImplicitUsing(
                        item.EvaluatedInclude,
                        aliasMetadata,
                        isStaticMetadata is not null && bool.TryParse(isStaticMetadata, out bool b) && b,
                        item.IsImported);
                }).ToList();
        });

        return usings;
    }

    private async Task<string> GetImplicitUsingsStringAsync()
    {
        List<ImplicitUsing> usings = await GetImplicitUsingsAsync();
        return JsonConvert.SerializeObject(usings);
    }
    
    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetImplicitUsingsStringAsync();
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetImplicitUsingsStringAsync();
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        if (JsonConvert.DeserializeObject(unevaluatedPropertyValue, typeof(List<ImplicitUsing>)) is not List<ImplicitUsing> usingsToSet)
        {
            return null;
        }

        usingsToSet.RemoveAll(usingToSet => usingToSet.IsReadOnly);

        List<ImplicitUsing> existingUsings = await GetImplicitUsingsAsync();
        
        foreach (ImplicitUsing existingUsing in existingUsings)
        {
            if (!usingsToSet.Any(usingToSet => usingToSet.Include.Equals(existingUsing.Include)))
            {
                try
                {
                    await _projectAccessor.OpenProjectForWriteAsync(_configuredProject, project =>
                    {
                        project.RemoveItems(
                            project.GetItems(UsingItemType).Where(i => string.Equals(existingUsing.Include, i.EvaluatedInclude, StringComparisons.ItemNames) && !i.IsImported)
                        );
                    });

                }
                catch (Exception ex)
                {
                    // If there's a race condition we can't remove it. otherwise throw
                    if (ex is not ArgumentException && ex is not InvalidOperationException)
                    {
                        throw;
                    }
                }
            }
        }

        foreach (ImplicitUsing existingUsing in existingUsings)
        {
            if (usingsToSet.Any(usingToSet => usingToSet.Equals(existingUsing)))
            {
                usingsToSet.Remove(existingUsing);
            }
        }
        
        // Verify we have at least one valid import to add before acquiring a write lock.
        if (usingsToSet.Any(importToAdd => importToAdd.Include.Length > 0))
        {
            await _projectAccessor.OpenProjectForWriteAsync(_configuredProject, project =>
            {
                foreach (ImplicitUsing usingToSet in usingsToSet)
                {

                    List<ImplicitUsing> existingUsingsOfIncludeToRemove = existingUsings.FindAll(existingUsing =>
                        string.Equals(usingToSet.Include, existingUsing.Include, StringComparisons.PropertyValues) && !existingUsing.IsReadOnly && !usingToSet.Equals(existingUsing));

                    if (existingUsingsOfIncludeToRemove.Count > 0)
                    {
                        project.RemoveItems(
                            project.GetItems(UsingItemType)
                                .Where(i => string.Equals(usingToSet.Include, i.EvaluatedInclude, StringComparisons.ItemNames) && !i.IsImported)
                        );
                    }
                    
                    if (usingToSet.Include.Length > 0 && (usingToSet.Alias is null || usingToSet.Alias.Length > 0))
                    {
                        var usingMetadata = new List<KeyValuePair<string, string>>();
                        if (usingToSet.Alias is { } alias)
                        {
                            usingMetadata.Add(new KeyValuePair<string, string>("Alias", alias));
                        }

                        if (usingToSet.IsStatic)
                        {
                            usingMetadata.Add(new KeyValuePair<string, string>("Static", usingToSet.IsStatic.ToString()));
                        }
                        
                        project.AddItem(UsingItemType, usingToSet.Include, usingMetadata);
                    }
                }
            });
        }

        return null;
    }
}
