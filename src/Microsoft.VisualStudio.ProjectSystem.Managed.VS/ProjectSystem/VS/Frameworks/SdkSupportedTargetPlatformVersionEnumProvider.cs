// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Responsible for producing valid values for the SdkSupportedTargetPlatformVersion property from evaluation.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SdkSupportedTargetPlatformVersionEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SdkSupportedTargetPlatformVersionEnumProvider : SingleRuleSupportedValuesProvider
    {
        [ImportingConstructor]
        public SdkSupportedTargetPlatformVersionEnumProvider(
            ConfiguredProject project, 
            IProjectSubscriptionService subscriptionService) 
            : base(project, subscriptionService, ruleName: SdkSupportedTargetPlatformVersion.SchemaName) {}

        protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new PageEnumValue(new EnumValue()
            {
                // Example: <SdkSupportedTargetPlatformVersion Include="7.0"/>
                //          <SdkSupportedTargetPlatformVersion Include="8.0"/>

                Name = item.Key
            });
        }

        protected override int SortValues(IEnumValue a, IEnumValue b)
        {
            return NaturalStringComparer.Instance.Compare(a.Name, b.Name);
        }
    }
}
