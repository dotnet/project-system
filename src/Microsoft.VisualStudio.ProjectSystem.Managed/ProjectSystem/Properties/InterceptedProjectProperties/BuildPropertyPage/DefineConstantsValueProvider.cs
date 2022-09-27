// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

//[ExportInterceptingPropertyValueProvider(ConfiguredBrowseObject.DefineConstantsProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
//[AppliesTo("(!" + ProjectCapabilities.VB + ")")]
internal class DefineConstantsValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly KeyValuePairListEncoding _encoding = new();

    internal const string DefineConstantsRecursivePrefix = "$(DefineConstants)";
    
    internal static IEnumerable<string> ParseDefinedConstantsFromUnevaluatedValue(string unevaluatedValue)
    {
        return unevaluatedValue.Length <= DefineConstantsRecursivePrefix.Length 
            ? Array.Empty<string>() 
            : unevaluatedValue.Substring(DefineConstantsRecursivePrefix.Length).Split(';');
    }

    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        // if DefineConstants has not been defined in the project file, nothing has been selected.
        if (!await IsValueDefinedInContextAsync(ConfiguredBrowseObject.DefineConstantsProperty, defaultProperties))
        {
            return string.Empty;
        }

        return _encoding.Format(
            ParseDefinedConstantsFromUnevaluatedValue(unevaluatedPropertyValue)
                .Select(symbol => (Key: symbol, Value: bool.FalseString))
        );
    }

    public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        string encodedSymbolsToWrite = string.Join(
            ";",
            _encoding.Parse(unevaluatedPropertyValue)
                .Select(pair => pair.Name)
                .Distinct()
        );

        return Task.FromResult<string?>($"{DefineConstantsRecursivePrefix}{encodedSymbolsToWrite}");
    }
}
