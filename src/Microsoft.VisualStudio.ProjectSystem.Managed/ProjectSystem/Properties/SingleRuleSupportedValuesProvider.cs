// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal abstract class SingleRuleSupportedValuesProvider : SupportedValuesProvider
    {
        /// <summary>
        /// Specifies if a 'None' value should be added to the resulting list.
        /// </summary>
        private readonly bool _useNoneValue;

        private readonly string _ruleName;

        protected sealed override string[] RuleNames => new string[] { _ruleName };

        protected SingleRuleSupportedValuesProvider(ConfiguredProject project, IProjectSubscriptionService subscriptionService, string ruleName, bool useNoneValue = false) : base(project, subscriptionService)
        {
            _ruleName = ruleName;
            _useNoneValue = useNoneValue;
        }

        protected override EnumCollection Transform(IProjectSubscriptionUpdate input)
        {
            IProjectRuleSnapshot snapshot = input.CurrentState[_ruleName];

            int capacity = snapshot.Items.Count + (_useNoneValue ? 1 : 0);
            var list = new List<IEnumValue>(capacity);

            if (_useNoneValue)
            {
                list.Add(new PageEnumValue(new EnumValue
                {
                    Name = string.Empty,
                    DisplayName = Resources.Property_NoneValue
                }));
            }

            list.AddRange(snapshot.Items.Select(ToEnumValue));
            list.Sort(SortValues); // TODO: This is a hotfix for item ordering. Remove this when completing: https://github.com/dotnet/project-system/issues/7025
            return list;
        }
    }
}
