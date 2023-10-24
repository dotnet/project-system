// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

[ExportDynamicEnumValuesProvider("TrimModeEnumProvider")]
[AppliesTo(ProjectCapability.DotNet)]
internal class TrimModeEnumProvider : IDynamicEnumValuesProvider
{
    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(new TrimModeEnumGenerator());
    }

    private class TrimModeEnumGenerator : IDynamicEnumValuesGenerator
    {
        private static readonly List<IEnumValue> s_enumValues = new()
        {
            new PageEnumValue(new EnumValue { Name = string.Empty, DisplayName = "(Default)" }),
            new PageEnumValue(new EnumValue { Name = "none", DisplayName = "None" }),
            new PageEnumValue(new EnumValue { Name = "full", DisplayName = "Full" })
        };

        public bool AllowCustomValues => true;

        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return Task.FromResult<ICollection<IEnumValue>>(s_enumValues);
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            return Task.FromResult<IEnumValue?>(value);
        }
    }
}
