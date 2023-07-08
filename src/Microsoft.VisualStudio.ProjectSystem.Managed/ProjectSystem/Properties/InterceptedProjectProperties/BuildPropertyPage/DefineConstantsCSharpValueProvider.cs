// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(ConfiguredBrowseObject.DefineConstantsProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.CSharpOrFSharp)]
internal class DefineConstantsValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly IProjectAccessor _projectAccessor;
    private readonly ConfiguredProject _project;

    internal const string DefineConstantsRecursivePrefix = "$(DefineConstants)";

    [ImportingConstructor]
    public DefineConstantsValueProvider(IProjectAccessor projectAccessor, ConfiguredProject project)
    {
        _projectAccessor = projectAccessor;
        _project = project;
    }

    internal static IEnumerable<string> ParseDefinedConstantsFromUnevaluatedValue(string unevaluatedValue)
    {
        return unevaluatedValue.Length <= DefineConstantsRecursivePrefix.Length  || !unevaluatedValue.StartsWith(DefineConstantsRecursivePrefix)
            ? Array.Empty<string>() 
            : unevaluatedValue.Substring(DefineConstantsRecursivePrefix.Length).Split(';').Where(x => x.Length > 0);
    }

    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        string? unevaluatedDefineConstantsValue = await GetUnevaluatedDefineConstantsPropertyValueAsync();

        // if DefineConstants has not been defined in the project file, nothing has been selected.
        if (unevaluatedDefineConstantsValue is null)
        {
            return string.Empty;
        }

        return KeyValuePairListEncoding.Format(
            ParseDefinedConstantsFromUnevaluatedValue(unevaluatedDefineConstantsValue)
                .Select(symbol => (Key: symbol, Value: bool.FalseString))
        );
    }

    // We cannot rely on the unevaluated property value as obtained through Project.GetProperty.UnevaluatedValue - the reason is that for a recursively-defined
    // property, this may not return the value locally defined. Instead, we can walk back up the property's predecessors (where it comes from) until we reach the project file.
    // If we don't find a property that's not been imported, then we can determine that this property has not been defined locally. This is used in two place:
    // 1. instead of unevaluatedPropertyValue, and
    // 2. to override IsValueDefinedInContextAsync, as this will always return false
    private async Task<string?> GetUnevaluatedDefineConstantsPropertyValueAsync()
    {
        await ((ConfiguredProject2)_project).EnsureProjectEvaluatedAsync();
        return await _projectAccessor.OpenProjectForReadAsync(_project, project =>
        {
            project.ReevaluateIfNecessary();
            ProjectProperty defineConstantsProperty = project.GetProperty(ConfiguredBrowseObject.DefineConstantsProperty);
            while (defineConstantsProperty.IsImported && defineConstantsProperty.Predecessor is not null)
            {
                defineConstantsProperty = defineConstantsProperty.Predecessor;
            }

            return defineConstantsProperty.IsImported ? null : defineConstantsProperty.UnevaluatedValue;
        });
    }

    public override async Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
    {
        return await GetUnevaluatedDefineConstantsPropertyValueAsync() is null; // if we have a non-imported unevaluated value, it's been defined locally.
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        // constants recursively obtained from above in this property's hierarchy (from imported files)
        IEnumerable<string> innerConstants =
            ParseDefinedConstantsFromUnevaluatedValue(await defaultProperties.GetUnevaluatedPropertyValueAsync(ConfiguredBrowseObject.DefineConstantsProperty) ?? string.Empty);

        IEnumerable<string> constantsToWrite = KeyValuePairListEncoding.Parse(unevaluatedPropertyValue)
            .Select(pair => pair.Name)
            .Where(x => !innerConstants.Contains(x))
            .Distinct()
            .ToList();

        if (!constantsToWrite.Any())
        {
            await defaultProperties.DeletePropertyAsync(propertyName, dimensionalConditions);
            return null;
        }

        return $"{DefineConstantsRecursivePrefix};" + string.Join(";", constantsToWrite);
    }
}
