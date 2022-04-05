// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Responsible for producing valid values for the SdkSupportedTargetPlatformIdentifier property from evaluation.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SdkSupportedTargetPlatformIdentifierEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SdkSupportedTargetPlatformIdentifierEnumProvider : SingleRuleSupportedValuesProvider
    {
        [ImportingConstructor]
        public SdkSupportedTargetPlatformIdentifierEnumProvider(
            ConfiguredProject project,
            IProjectSubscriptionService subscriptionService)
            : base(project, subscriptionService, ruleName: SdkSupportedTargetPlatformIdentifier.SchemaName, useNoneValue: true) { }

        protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new PageEnumValue(new EnumValue()
            {
                // Example: <SdkSupportedTargetPlatformIdentifier Include="windows" DisplayName="Windows"/>
                //          <SdkSupportedTargetPlatformIdentifier Include="ios" DisplayName="iOS"/>

                Name = item.Key,
                DisplayName = item.Value[SdkSupportedTargetPlatformIdentifier.DisplayNameProperty]
            });
        }

        protected override int SortValues(IEnumValue a, IEnumValue b)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(a.DisplayName, b.DisplayName);
        }
    }
}
