// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(ConfiguredBrowseObject.DefineConstantsProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.CSharpOrFSharp)]
[method: ImportingConstructor]
internal class DefineConstantsCAndFSharpValueProvider(IProjectAccessor projectAccessor, ConfiguredProject project) : InterceptingPropertyValueProviderBase
{
    private const string DefineConstantsRecursivePrefix = "$(DefineConstants)";

    private static IEnumerable<string> ParseDefinedConstantsFromUnevaluatedValue(string unevaluatedValue)
    {
        string substring = unevaluatedValue.Length <= DefineConstantsRecursivePrefix.Length  || !unevaluatedValue.StartsWith(DefineConstantsRecursivePrefix)
            ? unevaluatedValue
            : unevaluatedValue.Substring(DefineConstantsRecursivePrefix.Length);

        return substring.Split(';').Where(x => x.Length > 0);
    }

    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        string? unevaluatedDefineConstantsValue = await GetUnevaluatedDefineConstantsPropertyValueAsync();

        // if DefineConstants has not been defined in the project file, nothing has been selected.
        if (unevaluatedDefineConstantsValue is null)
        {
            return string.Empty;
        }

        var pairs = KeyValuePairListEncoding.Parse(unevaluatedDefineConstantsValue, separator: ';').Select(pair => pair.Name)
            .Where(symbol => !string.IsNullOrEmpty(symbol))
            .Select(symbol => (symbol, bool.FalseString)).ToList();
        
        return KeyValuePairListEncoding.Format(pairs, separator: ',');
    }

    // We cannot rely on the unevaluated property value as obtained through Project.GetProperty.UnevaluatedValue - the reason is that for a recursively-defined
    // property, this may not return the value locally defined. Instead, we can walk back up the property's predecessors (where it comes from) until we reach the project file.
    // If we don't find a property that's not been imported, then we can determine that this property has not been defined locally. This is used in two place:
    // 1. instead of unevaluatedPropertyValue, and
    // 2. to override IsValueDefinedInContextAsync, as this will always return false
    private async Task<string?> GetUnevaluatedDefineConstantsPropertyValueAsync()
    {
        await ((ConfiguredProject2)project).EnsureProjectEvaluatedAsync();

        return await projectAccessor.OpenProjectForReadAsync(project, msbuildProject =>
        {
            msbuildProject.ReevaluateIfNecessary();
            ProjectProperty defineConstantsProperty = msbuildProject.GetProperty(ConfiguredBrowseObject.DefineConstantsProperty);
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

        var foundConstants = KeyValuePairListEncoding.Parse(unevaluatedPropertyValue, separator: ',') 
            .Select(pair => pair.Name)
            .Where(pair => !string.IsNullOrEmpty(pair))
            .Select(constant => constant.Trim(';')) // trim any leading or trailing semicolons, because we will add our own separating semicolons
            .Where(constant => !string.IsNullOrEmpty(constant)) // you aren't allowed to add a semicolon as a constant
            .Distinct()
            .ToList();

        var writeableConstants = foundConstants.Where(constant => !innerConstants.Contains(constant)).ToList();
        if (writeableConstants.Count == 0)
        {
            await defaultProperties.DeletePropertyAsync(propertyName, dimensionalConditions);
            return null;
        }
        
        return string.Join(";", writeableConstants);
    }
}
