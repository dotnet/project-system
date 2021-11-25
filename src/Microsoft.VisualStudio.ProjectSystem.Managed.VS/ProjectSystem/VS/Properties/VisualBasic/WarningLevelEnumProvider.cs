// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    [ExportDynamicEnumValuesProvider("WarningLevelEnumProvider")]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class WarningLevelEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly Dictionary<string, IEnumValue> _persistWarningLevelEnumValues = new()
        {
                { nameof(prjWarningLevel.prjWarningLevel0), new PageEnumValue(new EnumValue {Name = "0" }) },
                { nameof(prjWarningLevel.prjWarningLevel1), new PageEnumValue(new EnumValue {Name = "1" }) },
                { nameof(prjWarningLevel.prjWarningLevel2), new PageEnumValue(new EnumValue {Name = "2" }) },
                { nameof(prjWarningLevel.prjWarningLevel3), new PageEnumValue(new EnumValue {Name = "3" }) },
                { nameof(prjWarningLevel.prjWarningLevel4), new PageEnumValue(new EnumValue {Name = "4" }) },
            };

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new MapDynamicEnumValuesProvider(_persistWarningLevelEnumValues));
        }
    }
}
