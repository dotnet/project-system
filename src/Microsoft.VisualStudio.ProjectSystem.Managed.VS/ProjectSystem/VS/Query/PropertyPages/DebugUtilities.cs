// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public static IEnumerable<Rule> GetDebugChildRules(IPropertyPagesCatalog projectCatalog)
        {
            foreach (string schemaName in projectCatalog.GetPropertyPagesSchemas(itemType: "LaunchProfile"))
            {
                if (projectCatalog.GetSchema(schemaName) is { PropertyPagesHidden: false, PageTemplate: CommandNameBasedDebuggerPageTemplate } possibleChildRule)
                {
                    yield return possibleChildRule;
                }
            }
        }
    }
}
