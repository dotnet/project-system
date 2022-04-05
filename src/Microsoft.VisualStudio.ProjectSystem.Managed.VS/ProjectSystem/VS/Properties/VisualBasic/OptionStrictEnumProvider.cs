// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    [ExportDynamicEnumValuesProvider("OptionStrictEnumProvider")]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class OptionStrictEnumProvider : IDynamicEnumValuesProvider
    {
        /// The value obtained from the EnumValue, when asked for value, is the <see cref="EnumValue.DisplayName"/>, unless otherwise
        /// a different overload is used to obtain the value of <see cref="EnumValue.Name"/>. Hence a provider usually needs just a map
        /// with a typical entry looking like, [ValuePersistedByUI] -> EnumValue {Name = [ValueToPersist] DisplayName = [ValueToReturnForGet]}
        /// In the case of OptionStrict, Compile Property Page explicitly gets the value from <see cref="EnumValue.Name"/> for the Enums and
        /// casts them to integer. This requires us to make <see cref="EnumValue.Name"/> as '0' and '1' and hence cannot reuse
        /// <see cref="_persistOptionStrictEnumValues"/> Values to be used when trying to retrieve value for the enum.
        private readonly ICollection<IEnumValue> _listedOptionStrictEnumValues = new List<IEnumValue>
            {
                new PageEnumValue(new EnumValue {Name = "0",    DisplayName = "Off", IsDefault = true }),
                new PageEnumValue(new EnumValue {Name = "1",    DisplayName = "On" })
            };

        private readonly Dictionary<string, IEnumValue> _persistOptionStrictEnumValues = new()
        {
                { "0",  new PageEnumValue(new EnumValue {Name = "Off" }) },
                { "1",  new PageEnumValue(new EnumValue {Name = "On" }) },
            };

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(
                new MapDynamicEnumValuesProvider(_persistOptionStrictEnumValues, _listedOptionStrictEnumValues));
        }
    }
}
