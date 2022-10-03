// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportDynamicEnumValuesProvider("DefineConstantsEnumProvider")]
[AppliesTo(ProjectCapability.CSharpOrFSharp)]
internal class DefineConstantsEnumProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
{
    [ImportingConstructor]
    public DefineConstantsEnumProvider()
    {
    }
    
    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(this);
    }

    public Task<ICollection<IEnumValue>> GetListedValuesAsync()
    {
        return Task.FromResult<ICollection<IEnumValue>>(new List<IEnumValue>());
    }

    public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
    {
        var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
        return Task.FromResult<IEnumValue?>(value);
    }

    public bool AllowCustomValues => true;
}
