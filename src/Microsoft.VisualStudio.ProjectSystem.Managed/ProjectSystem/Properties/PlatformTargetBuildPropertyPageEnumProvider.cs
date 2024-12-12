// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
///     Responsible for producing valid values for the <c>TargetPlatform</c> MSBuild property.
/// </summary>
/// <remarks>
///     Candidate values from the <c>AvailablePlatforms</c> MSBuild property.
/// </remarks>
[ExportDynamicEnumValuesProvider("PlatformTargetEnumProvider")]
[AppliesTo(ProjectCapability.DotNet)]
[method: ImportingConstructor]
internal sealed class PlatformTargetBuildPropertyPageEnumProvider(ProjectProperties properties) : IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
{
    public bool AllowCustomValues => false;

    public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
    {
        ConfigurationGeneral configuration = await properties.GetConfigurationGeneralPropertiesAsync();

        string availablePlatformsTargets = await configuration.AvailablePlatforms.GetDisplayValueAsync();

        List<IEnumValue> enumValues = [];
        HashSet<string> targets = new(StringComparers.ConfigurationDimensionValues);

        foreach (string platformTarget in new LazyStringSplit(availablePlatformsTargets, ','))
        {
            // Prevent duplicates.
            if (targets.Add(platformTarget))
            {
                enumValues.Add(new PageEnumValue(new EnumValue() { Name = platformTarget, DisplayName = GetDisplayName(platformTarget) }));
            }
        }

        return enumValues;

        static string GetDisplayName(string platformTarget)
        {
            const string AnyCpuPlatformName = "AnyCPU";
            const string AnyCpuDisplayName = "Any CPU";

            return platformTarget.Equals(AnyCpuPlatformName, StringComparisons.ConfigurationDimensionValues) ? AnyCpuDisplayName : platformTarget;
        }
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

