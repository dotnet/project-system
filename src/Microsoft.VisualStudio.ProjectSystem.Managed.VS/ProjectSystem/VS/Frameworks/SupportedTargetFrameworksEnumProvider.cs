// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Responsible for producing valid values for the TargetFramework property from evaluation.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworksEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SupportedTargetFrameworksEnumProvider : SupportedValuesProvider
    {
        protected override string[] RuleNames => new[] { SupportedNETCoreAppTargetFramework.SchemaName, SupportedNETFrameworkTargetFramework.SchemaName, SupportedNETStandardTargetFramework.SchemaName, ConfigurationGeneral.SchemaName };

        [ImportingConstructor]
        public SupportedTargetFrameworksEnumProvider(ConfiguredProject project, IProjectSubscriptionService subscriptionService)
            : base(project, subscriptionService)
        {
        }

        protected override EnumCollection Transform(IProjectSubscriptionUpdate input)
        {
            IProjectRuleSnapshot configurationGeneral = input.CurrentState[ConfigurationGeneral.SchemaName];

            string? targetFrameworkIdentifier = configurationGeneral.Properties.GetStringProperty(ConfigurationGeneral.TargetFrameworkIdentifierProperty);

            if (StringComparers.FrameworkIdentifiers.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetCoreApp))
            {
                return GetSupportedTargetFrameworksFromItems(SupportedNETCoreAppTargetFramework.SchemaName);
            }
            else if (StringComparers.FrameworkIdentifiers.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetFramework))
            {
                return GetSupportedTargetFrameworksFromItems(SupportedNETFrameworkTargetFramework.SchemaName);
            }
            else if (StringComparers.FrameworkIdentifiers.Equals(targetFrameworkIdentifier, TargetFrameworkIdentifiers.NetStandard))
            {
                return GetSupportedTargetFrameworksFromItems(SupportedNETStandardTargetFramework.SchemaName);
            }
            else
            {
                string? targetFramework = configurationGeneral.Properties.GetStringProperty(ConfigurationGeneral.TargetFrameworkProperty);
                string? targetFrameworkMoniker = configurationGeneral.Properties.GetStringProperty(ConfigurationGeneral.TargetFrameworkMonikerProperty);

                // This is the case where the TargetFrameworkProperty has a value we recognize but it's not in the supported lists the SDK sends us.
                // We decided we will show it in the UI.
                if (!Strings.IsNullOrEmpty(targetFramework))
                {
                    return new IEnumValue[]
                    {
                        new PageEnumValue(new EnumValue
                        {
                            Name = targetFrameworkMoniker ?? targetFramework,
                            DisplayName = targetFrameworkIdentifier ?? targetFramework
                        })
                    };
                }

                return Array.Empty<IEnumValue>();
            }

            EnumCollection GetSupportedTargetFrameworksFromItems(string ruleName)
            {
                IProjectRuleSnapshot snapshot = input.CurrentState[ruleName];

                int capacity = snapshot.Items.Count;
                var list = new List<IEnumValue>(capacity);

                list.AddRange(snapshot.Items.Select(ToEnumValue));
                list.Sort(SortValues); // TODO: This is a hotfix for item ordering. Remove this when completing: https://github.com/dotnet/project-system/issues/7025
                return list;
            }
        }

        protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new PageEnumValue(new EnumValue()
            {
                // Example: <SupportedTargetFramework  Include=".NETCoreApp,Version=v5.0"
                //                                     DisplayName=".NET 5.0" />

                Name = item.Key,
                DisplayName = item.Value[SupportedTargetFramework.DisplayNameProperty],
            });
        }

        protected override int SortValues(IEnumValue a, IEnumValue b)
        {
            return NaturalStringComparer.Instance.Compare(a.DisplayName, b.DisplayName);
        }
    }
}
