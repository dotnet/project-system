// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportDynamicEnumValuesProvider("OptionStrictEnumProvider")]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class OptionStrictEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Dictionary<string, IEnumValue> _listedOptionStrictEnumValues = new Dictionary<string, IEnumValue>
            {
                { "Off",    new PageEnumValue(new EnumValue {Name = "0",    DisplayName = "Off", IsDefault = true }) },
                { "On",     new PageEnumValue(new EnumValue {Name = "1",    DisplayName = "On" }) },
            };

        private readonly Dictionary<string, IEnumValue> _persistOptionStrictEnumValues = new Dictionary<string, IEnumValue>
            {
                { "0",  new PageEnumValue(new EnumValue {Name = "Off" }) },
                { "1",  new PageEnumValue(new EnumValue {Name = "On" }) },
            };

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(
                new MapDynamicEnumValuesProvider(_listedOptionStrictEnumValues, _persistOptionStrictEnumValues));
        }
    }
}