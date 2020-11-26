// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal static class DebugUtilities
    {
        public static string ConvertDebugPageNameToRealPageName(string pageName)
        {
            if (pageName.StartsWith("#debuggerParent#"))
            {
                return pageName.Substring("#debuggerParent#".Length);
            }
            else
            {
                return pageName;
            }
        }

        public static string ConvertRealPageNameToDebugPageName(string pageName)
        {
            return "#debuggerParent#" + pageName;
        }

        public static (string pageName, string categoryName) ConvertDebugPageAndCategoryToRealPageAndCategory(string pageName, string categoryName)
        {
            if (categoryName.StartsWith("#debuggerChild#"))
            {
                categoryName = categoryName.Substring("#debuggerChild#".Length);

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
            return "#debuggerChild#" + pageName + "#" + categoryName;
        }

        public static (string pageName, string propertyName) ConvertDebugPageAndPropertyToRealPageAndProperty(string pageName, string propertyName)
        {
            if (propertyName.StartsWith("#debuggerChild#"))
            {
                propertyName = propertyName.Substring("#debuggerChild#".Length);

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
            return "#debuggerChild#" + pageName + "#" + propertyName;
        }

        public static IEnumerable<Rule> GetDebugChildRules(IPropertyPagesCatalog projectCatalog)
        {
            foreach (string schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
            {
                if (projectCatalog.GetSchema(schemaName) is Rule possibleChildRule
                    && !possibleChildRule.PropertyPagesHidden
                    && possibleChildRule.PageTemplate == "commandNameBasedDebugger")
                {
                    yield return possibleChildRule;
                }
            }
        }
    }
}
