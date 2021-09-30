// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportDynamicEnumValuesProvider("AvailablePlatformTargets")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class AvailablePlatformTargetsProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
    {
        private readonly ProjectProperties _properties;

        public bool AllowCustomValues => false;

        [ImportingConstructor]
        public AvailablePlatformTargetsProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();

            string? availablePlatformsValues = (string?)await configuration.AvailablePlatforms.GetValueAsync();

            var result = new List<IEnumValue>();

            // AnyCPU, x64, x86
            if (availablePlatformsValues != null)
            {
                List<string> availablePlatformsList = availablePlatformsValues.Split(',').ToList();
                
                foreach(string item in availablePlatformsList)
                {
                    if (item == "AnyCPU")
                    {
                        result.Add(new PageEnumValue(new EnumValue()
                        {
                            Name = item,
                            DisplayName= "Any CPU"
                        }));
                    }

                    else
                    {
                        result.Add(new PageEnumValue(new EnumValue()
                        {
                            Name = item,
                            DisplayName = item
                        }));
                    }
                }
            }

            return result;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(this);
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();
    }
}
