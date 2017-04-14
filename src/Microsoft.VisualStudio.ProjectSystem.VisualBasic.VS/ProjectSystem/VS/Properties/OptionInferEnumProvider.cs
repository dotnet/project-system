// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportDynamicEnumValuesProvider("OptionInferEnumProvider")]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class OptionInferEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Dictionary<string, IEnumValue> _listedOptionInferEnumValues = new Dictionary<string, IEnumValue>
            {
                { "Off",    new PageEnumValue(new EnumValue {Name = "Off",  DisplayName = "Off" }) },
                { "On",     new PageEnumValue(new EnumValue {Name = "On",   DisplayName = "On", IsDefault = true }) },
            };

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new MapDynamicEnumValuesProvider(_listedOptionInferEnumValues));
        }
    }
}