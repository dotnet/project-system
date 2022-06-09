// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Rules;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
/// Provides the available <c>Page</c> items that may be set as the "Startup URI"
/// for a WPF application.
/// </summary>
[ExportDynamicEnumValuesProvider(nameof(PageItemEnumProvider))]
[AppliesTo(ProjectCapability.DotNet)]
internal class PageItemEnumProvider : SingleRuleSupportedValuesProvider
{
    [ImportingConstructor]
    public PageItemEnumProvider(
        ConfiguredProject project,
        IProjectSubscriptionService subscriptionService)
        : base(project, subscriptionService, PageItemRuleProvider.RuleName, useNoneValue: false)
    {
    }

    protected override int SortValues(IEnumValue a, IEnumValue b)
    {
        return StringComparers.Paths.Compare(a.DisplayName, b.DisplayName);
    }

    protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
    {
        // TODO: Only include items that are defined under the project root, that is, the
        // ones that can be expressed as a relative path from the project directory.
        string value = ContainingProject!.TryMakeRelativeToProjectDirectory(item.Key, out string? relativePath)
            ? relativePath
            : item.Key;

        return new PageEnumValue(new EnumValue()
        {
            Name = value
        });
    }
}
