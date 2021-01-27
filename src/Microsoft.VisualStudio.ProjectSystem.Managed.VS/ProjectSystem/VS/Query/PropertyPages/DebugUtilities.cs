// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Helper methods to convert back and forth between the page, category, and property names
    /// used by the Debug page in the Project Query API, and the names used by the underlying
    /// property system.
    /// </summary>
    internal static class DebugUtilities
    {
        private const string CommandNameBasedDebuggerPageTemplate = "commandNameBasedDebugger";
        private const string DebuggerParentPrefix = "#debuggerParent#";
        private const string DebuggerChildPrefix = "#debuggerChild#";

        public static string ConvertDebugPageNameToRealPageName(string pageName)
        {
            if (pageName.StartsWith(DebuggerParentPrefix))
            {
                return pageName.Substring(DebuggerParentPrefix.Length);
            }
            else
            {
                return pageName;
            }
        }

        public static string ConvertRealPageNameToDebugPageName(string pageName)
        {
            return DebuggerParentPrefix + pageName;
        }

        public static (string pageName, string categoryName) ConvertDebugPageAndCategoryToRealPageAndCategory(string pageName, string categoryName)
        {
            if (categoryName.StartsWith(DebuggerChildPrefix))
            {
                categoryName = categoryName.Substring(DebuggerChildPrefix.Length);

                int index = categoryName.IndexOf("#");
                if (index >= 0)
                {
                    pageName = categoryName.Substring(0, index);
                    categoryName = categoryName.Substring(index + 1);
                }
            }

            return (pageName, categoryName);
        }

        public static string ConvertRealPageAndCategoryToDebugCategory(string pageName, string categoryName)
        {
            return DebuggerChildPrefix + pageName + "#" + categoryName;
        }

        public static (string pageName, string propertyName) ConvertDebugPageAndPropertyToRealPageAndProperty(string pageName, string propertyName)
        {
            if (propertyName.StartsWith(DebuggerChildPrefix))
            {
                propertyName = propertyName.Substring(DebuggerChildPrefix.Length);

                int index = propertyName.IndexOf("#");
                if (index >= 0)
                {
                    pageName = propertyName.Substring(0, index);
                    propertyName = propertyName.Substring(index + 1);
                }
            }

            return (pageName, propertyName);
        }

        public static string ConvertRealPageAndPropertyToDebugProperty(string pageName, string propertyName)
        {
            return DebuggerChildPrefix + pageName + "#" + propertyName;
        }

        public static IEnumerable<Rule> GetDebugChildRules(IPropertyPagesCatalog projectCatalog)
        {
            foreach (string schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
            {
                if (projectCatalog.GetSchema(schemaName) is Rule possibleChildRule
                    && !possibleChildRule.PropertyPagesHidden
                    && possibleChildRule.PageTemplate == CommandNameBasedDebuggerPageTemplate)
                {
                    yield return possibleChildRule;
                }
            }
        }

        public static string? GetDebugCategoryNameOrNull(Rule rule, Category category)
        {
            return GetDebugCategoryNameOrNull(rule, category.Name);
        }

        public static string? GetDebugCategoryNameOrNull(Rule rule, string categoryName)
        {
            if (rule.PageTemplate == CommandNameBasedDebuggerPageTemplate)
            {
                return ConvertRealPageAndCategoryToDebugCategory(rule.Name, categoryName);
            }

            return null;
        }

        public static string? GetDebugPropertyNameOrNull(BaseProperty property)
        {
            if (property.ContainingRule.PageTemplate == CommandNameBasedDebuggerPageTemplate)
            {
                return ConvertRealPageAndPropertyToDebugProperty(property.ContainingRule.Name, property.Name);
            }

            return null;
        }

        public static string? UpdateDebuggerDependsOnMetadata(Rule containingRule, string? dependsOnString)
        {
            if (containingRule.PageTemplate == CommandNameBasedDebuggerPageTemplate)
            {
                dependsOnString = dependsOnString is not null
                    ? dependsOnString + ";"
                    : string.Empty;

                dependsOnString = dependsOnString + "ParentDebugPropertyPage::ActiveLaunchProfile;ParentDebugPropertyPage::LaunchTarget";
            }

            return dependsOnString;
        }

        public static string? UpdateDebuggerVisibilityConditionMetadata(string? visibilityCondition, Rule rule)
        {
            if (rule.PageTemplate == CommandNameBasedDebuggerPageTemplate)
            {
                var commandNameCondition = $"(eq (evaluated \"ParentDebugPropertyPage\" \"LaunchTarget\") \"{rule.Name}\")";
                visibilityCondition = visibilityCondition is not null
                    ? $"(and {visibilityCondition} {commandNameCondition})"
                    : commandNameCondition;
            }

            return visibilityCondition;
        }
    }
}
