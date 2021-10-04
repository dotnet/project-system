// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Threading;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Responsible for producing valid values for the TargetPlatform property from a design-time build.
    /// </summary>
    [ExportDynamicEnumValuesProvider("PlatformTargetEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class PlatformTargetBuildPropertyPageEnumProvider : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public PlatformTargetBuildPropertyPageEnumProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public bool AllowCustomValues => false;

        public async Task<EnumCollection> GetListedValuesAsync()
        {
            var result = new List<IEnumValue>();

            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();

            string availablePlatformsTargets = await configuration.AvailablePlatforms.GetDisplayValueAsync();

            foreach (string platformTarget in availablePlatformsTargets.Split(','))
            {
                result.Add(new PageEnumValue(new EnumValue() { Name = platformTarget, DisplayName = platformTarget.Equals("AnyCPU") ? "Any CPU" : platformTarget }));
            }

            return result;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(this);
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            return TaskResult.Null<IEnumValue>();
        }
    }
}

