// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class ProjectPropertiesLocalizationRuleTests : XamlRuleTestBase
    {
        [Theory]
        [MemberData(nameof(GetPropertyPagesRules))]
        public void CategoriesShouldBeDefinedOnFile(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);
            IEnumerable<string> categories = GetCategories();
            var propertyElements = rule.XPathSelectElements(@"/msb:Rule", namespaceManager).Elements();

            // If the page is an extension, we have to check the base page for categories.            
            if (ruleName.Contains(".CSharp"))
                categories.Union(GetCategories(".CSharp")).Distinct();
            
            else if (ruleName.Contains(".VisualBasic"))
                categories.Union(GetCategories(".VisualBasic")).Distinct();
            
            else if (ruleName.Contains(".FSharp"))
                categories.Union(GetCategories(".FSharp")).Distinct();

            foreach (XElement element in propertyElements)
            {
                // Skip these xml elements.
                if (element.Name.LocalName == "Rule.Categories" || element.Name.LocalName == "Rule.DataSource" || element.Name.LocalName == "Rule.Metadata")
                    continue;

                // Skip if the property is not visible.
                if (element.Attribute("Visible") is null || element.Attribute("Visible").Value == "False")
                    continue;

                var categoryAttribute = element.Attribute("Category");

                // Ensure properties have a category attribute with a value.
                Assert.True(categoryAttribute is not null, $"Rule '{ruleName}' has property '{element.Attribute("Name").Value}' with no category. Please add a category to the property.");
                Assert.True(!string.IsNullOrEmpty(categoryAttribute.Value), $"Rule '{ruleName}' has a property '{element.Attribute("Name").Value}' with an empty category. Please add a value to the category to the property.");

                // Ensure the category value is defined in the set of categories.
                Assert.True(categories.Contains(categoryAttribute.Value), $"Rule '{ruleName}' has a property '{element.Attribute("Name").Value}' with a category '{categoryAttribute.Value}' that is not defined in the set of categories. Please add the category to the set of categories in the Rule.Categories.");
            }

            IEnumerable<string> GetCategories(string ruleExtension = "")
            {
                try
                {
                    XElement baseRule = LoadXamlRule(fullPath.Remove(fullPath.IndexOf(ruleExtension), ruleExtension.Length), out var baseNamespaceManager);
                    return baseRule.XPathSelectElements(@"/msb:Rule/msb:Rule.Categories/msb:Category", baseNamespaceManager).Select(e => e.Attribute("Name").Value).ToList();
                } 
                catch (FileNotFoundException)
                {
                    // It's okay if there's no base file, let's just return an empty list.
                    return new List<string>();
                }
            }
        }

        public static IEnumerable<object[]> GetPropertyPagesRules()
        {
            return Project(GetRules("PropertyPages"));
        }
    }
}
