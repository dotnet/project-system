// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider("ImplicitUsingsEditor", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.CSharp)]
internal class ImplicitUsingsValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly IProjectAccessor _projectAccessor;
    private readonly ConfiguredProject _configuredProject;
    
    [ImportingConstructor]
    public ImplicitUsingsValueProvider(IProjectAccessor projectAccessor, ConfiguredProject configuredProject)
    {
        _projectAccessor = projectAccessor;
        _configuredProject = configuredProject;
    }

    private async Task<string> GetImplicitUsingsAsync()
    {
        List<ImplicitUsing> usings = await _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
        {
            return project
                .GetItems("Using")
                .Select(item =>
                {
                    string? isStaticMetadata = item.DirectMetadata.FirstOrDefault(metadata => metadata.Name.Equals("Static", StringComparison.Ordinal))?.EvaluatedValue;
                    string? aliasMetadata = item.DirectMetadata.FirstOrDefault(metadata => metadata.Name.Equals("Alias", StringComparison.Ordinal))?.EvaluatedValue;

                    return new ImplicitUsing(
                        item.EvaluatedInclude,
                        aliasMetadata,
                        isStaticMetadata is not null && bool.TryParse(isStaticMetadata, out bool b) && b,
                        item.IsImported);
                }).ToList();
        });

        return JsonConvert.SerializeObject(usings);
    }
    
    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetImplicitUsingsAsync();
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetImplicitUsingsAsync();
    }

    internal class ImplicitUsing
    {
        public string Include { get; set; }
        public string? Alias { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        
        public ImplicitUsing(string include, string? alias, bool isStatic, bool isReadOnly)
        {
            Include = include;
            Alias = alias;
            IsStatic = isStatic;
            IsReadOnly = isReadOnly;
        }
    }
}
