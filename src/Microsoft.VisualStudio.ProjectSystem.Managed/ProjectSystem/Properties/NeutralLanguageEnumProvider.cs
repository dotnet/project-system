// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties.Package;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportDynamicEnumValuesProvider(nameof(NeutralLanguageEnumProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class NeutralLanguageEnumProvider : IDynamicEnumValuesProvider
    {
        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new NeutralLanguageEnumGenerator());
        }

        private class NeutralLanguageEnumGenerator : IDynamicEnumValuesGenerator
        {
            public bool AllowCustomValues => false;

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var values = Enumerable.Empty<IEnumValue>()
                    .Append(new PageEnumValue(new EnumValue() { Name = NeutralLanguageValueProvider.NoneValue, DisplayName = Resources.Property_NoneValue }))
                    .Concat(CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures | CultureTypes.InstalledWin32Cultures)
                        .Where(info => info.Name.Length != 0)
                        .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(info => new PageEnumValue(new EnumValue() { Name = info.Name, DisplayName = string.Format(Resources.NeutralLanguage_DisplayNameFormatString, info.DisplayName, info.Name) })))
                    .ToArray();

                return Task.FromResult<ICollection<IEnumValue>>(values);
            }

            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();
        }
    }
}
