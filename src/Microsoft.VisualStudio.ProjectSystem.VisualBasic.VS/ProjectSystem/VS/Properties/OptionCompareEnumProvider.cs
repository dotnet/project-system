// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportDynamicEnumValuesProvider("OptionCompareEnumProvider")]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class OptionCompareEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Dictionary<string, IEnumValue> _listedOptionCompareEnumValues = new Dictionary<string, IEnumValue>
            {
                { "Binary", new PageEnumValue(new EnumValue {Name = "Binary",   DisplayName = "Binary", IsDefault = true }) },
                { "Text",   new PageEnumValue(new EnumValue {Name = "Text",     DisplayName = "Text" }) },
            };

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new MapDynamicEnumValuesProvider(_listedOptionCompareEnumValues));
        }
    }
}