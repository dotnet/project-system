// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportDynamicEnumValuesProvider("DefineConstantsEnumProvider2")]
[AppliesTo(ProjectCapability.DotNet)]
internal class DefineConstantsEnumProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
{
    private readonly IProjectProperties _defaultProperties;

    [ImportingConstructor]
    public DefineConstantsEnumProvider(IProjectProperties defaultProperties)
    {
        _defaultProperties = defaultProperties;
    }
    
    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(this);
    }

    public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
    {
        return DefineConstantsValueProvider.ParseDefinedConstantsFromUnevaluatedValue(await _defaultProperties.GetUnevaluatedPropertyValueAsync(ConfiguredBrowseObject.DefineConstantsProperty) ?? string.Empty)
            .Select(symbol => (IEnumValue)new PageEnumValue(new EnumValue { Name = symbol, DisplayName = symbol }))
            .ToList();
    }

    public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
    {
        var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
        return Task.FromResult<IEnumValue?>(value);
    }

    public bool AllowCustomValues => true;
}
