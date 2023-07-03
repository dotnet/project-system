// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

[ExportDynamicEnumValuesProvider("TrimmingEnumProvider")]
[AppliesTo(ProjectCapability.DotNet)]
internal class TrimmingEnumProvider : IDynamicEnumValuesProvider
{

    [ImportingConstructor]
    public TrimmingEnumProvider()
    {
    }

    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(new TrimmingEnumGenerator());
    }
}

internal class TrimmingEnumGenerator : IDynamicEnumValuesGenerator
{

    public bool AllowCustomValues => true;

    public TrimmingEnumGenerator()
    {
    }

    public Task<ICollection<IEnumValue>> GetListedValuesAsync()
    {
        List<IEnumValue> enumValues = new()
        {
            new PageEnumValue(new EnumValue { Name = "", DisplayName = "(Default)" }),
            new PageEnumValue(new EnumValue { Name = "none", DisplayName = "None" }),
            new PageEnumValue(new EnumValue { Name = "full", DisplayName = "Full" })
        };

        return Task.FromResult<ICollection<IEnumValue>>(enumValues);
    }

    public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
    {
        var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
        return Task.FromResult<IEnumValue?>(value);
    }
}
