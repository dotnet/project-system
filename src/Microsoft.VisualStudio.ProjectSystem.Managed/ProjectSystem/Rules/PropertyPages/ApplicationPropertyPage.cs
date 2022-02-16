// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using PPR = Microsoft.VisualStudio.ProjectSystem.Rules.PropertyPages.PropertyPageResources;

namespace Microsoft.VisualStudio.ProjectSystem.Rules.PropertyPages
{
    internal static class PropertyMetadata
    {
        public static readonly string VisibilityCondition = "VisibilityCondition";
        public static readonly string SearchTerms = "SearchTerms";
        public static readonly string DependsOn = "DependsOn";
    }

    // TODO: The context could come from a constant.
    [ExportRuleObjectProvider(name: "DefaultApplicationPropertyPageProvider", context: "Project")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    [Order(Order.Default)]
    internal sealed class ApplicationPropertyPageProvider : IRuleObjectProvider
    {
        private const string GeneralCategoryName = "General";
        private const string ResourcesCategoryName = "Resources";
        private const string PackagingCategoryName = "Packaging";

        private readonly Lazy<List<Rule>> _rules = new(CreateRules);

        private static List<Rule> CreateRules()
        {
            Rule rule = new();

            rule.BeginInit();

            rule.Name = "Application";
            rule.Description = PPR.ApplicationPage_Description;
            rule.DisplayName = PPR.ApplicationPage_DisplayName;
            // TODO: The PageTemplate could come from a constant.
            rule.PageTemplate = "generic";
            rule.Order = 100;

            rule.Categories = new()
            {
                CreateCategory(
                    name: GeneralCategoryName,
                    displayName: PPR.ApplicationPage_GeneralCategory_DisplayName,
                    description: PPR.ApplicationPage_GeneralCategory_Description),
                CreateCategory(
                    name: ResourcesCategoryName,
                    displayName: PPR.ApplicationPage_ResourcesCategory_DisplayName,
                    description: PPR.ApplicationPage_ResourcesCategory_Description),
                CreateCategory(
                    name: PackagingCategoryName,
                    displayName: PPR.ApplicationPage_PackagingCategory_DisplayName,
                    description: PPR.ApplicationPage_PackagingCategory_Description)
            };

            rule.DataSource = new()
            {
                // TODO: The Persistence could come from a constant.
                Persistence = "ProjectFile",
                SourceOfDefaultValue = DefaultValueSourceLocation.AfterContext,
                HasConfigurationCondition = false
            };

            EnumProperty outputTypeProperty = new()
            {
                Name = "OutputType",
                DisplayName = PPR.ApplicationPage_OutputTypeProperty_DisplayName,
                Description = PPR.ApplicationPage_OutputTypeProperty_Description,
                Category = GeneralCategoryName,
                AdmissibleValues =
                {
                    new() { Name = "Library", DisplayName = PPR.ApplicationPage_OutputTypeProperty_LibraryValue_DisplayName },
                    new() { Name = "Exe", DisplayName = PPR.ApplicationPage_OutputTypeProperty_ExeValue_DisplayName },
                    new() { Name = "WinExe", DisplayName = PPR.ApplicationPage_OutputTypeProperty_WinExeValue_DisplayName }
                }
            };

            BoolProperty targetMultipleFrameworksProperty = new()
            {
                Name = "TargetMultipleFrameworks",
                DisplayName = PPR.ApplicationPage_TargetMultipleFrameworksProperty_DisplayName,
                Description = PPR.ApplicationPage_TargetMultipleFrameworksProperty_Description,
                HelpUrl = "https://go.microsoft.com/fwlink/?linkid=2147236",
                Category = GeneralCategoryName,
                Visible = false,
                DataSource = new()
                {
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                }
            };

            DynamicEnumProperty interceptedTargetFrameworkProperty = new()
            {
                Name = "InterceptedTargetFramework",
                DisplayName = PPR.ApplicationPage_InterceptedTargetFrameworkProperty_DisplayName,
                Description = PPR.ApplicationPage_InterceptedTargetFrameworkProperty_Description,
                HelpUrl = "https://go.microsoft.com/fwlink/?linkid=2147236",
                Category = GeneralCategoryName,
                EnumProvider = "SupportedTargetFrameworksEnumProvider",
                MultipleValuesAllowed = false,
                DataSource = new()
                {
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                },
                Metadata =
                {
                    new() { Name = PropertyMetadata.VisibilityCondition, Value = "(not (has-evaluated-value \"Application\" \"TargetMultipleFrameworks\" true))" },
                    new() { Name = PropertyMetadata.SearchTerms, Value = PPR.ApplicationPage_InterceptedTargetFrameworkProperty_SearchTerms },
                    new() { Name = PropertyMetadata.DependsOn, Value = "Application::TargetMultipleFrameworks" }
                }
            };

            StringProperty targetFrameworksProperty = new()
            {
                Name = "TargetFrameworks",
                DisplayName = PPR.ApplicationPage_TargetFrameworksProperty_DisplayName,
                Description = PPR.ApplicationPage_TargetFrameworksProperty_Description,
                HelpUrl = "https://go.microsoft.com/fwlink/?linkid=2147236",
                Category = GeneralCategoryName,
                DataSource = new()
                {
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                },
                Metadata =
                {
                    new() { Name = PropertyMetadata.VisibilityCondition, Value = "(has-evaluated-value \"Application\" \"TargetMultipleFrameworks\" true)" },
                    new() { Name = PropertyMetadata.SearchTerms, Value = PPR.ApplicationPage_TargetFrameworksProperty_SearchTerms },
                    new() { Name = PropertyMetadata.DependsOn, Value = "Application::TargetMultipleFrameworks" }
                }
            };

            StringProperty installOtherFrameworksProperty = new()
            {
                Name = "InstallOtherFrameworks",
                DisplayName = PPR.ApplicationPage_InstallOtherFrameworksProperty_DisplayName,
                Category = GeneralCategoryName,
                DataSource = new()
                {
                    PersistedName = "InstallOtherFrameworks",
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                },
                ValueEditors =
                {
                    new()
                    {
                        // TODO: EditorType could come from a constant
                        EditorType = "LinkAction",
                        Metadata =
                        {
                            new() { Name = "Action", Value = "URL" },
                            new() { Name = "URL", Value = "http://go.microsoft.com/fwlink/?LinkID=287120" }
                        }
                    }
                }
            };

            DynamicEnumProperty targetPlatformIdentifierProperty = new()
            {
                Name = "TargetPlatformIdentifier",
                DisplayName = PPR.ApplicationPage_TargetPlatformIdentifierProperty_DisplayName,
                Description = PPR.ApplicationPage_TargetPlatformIdentifierProperty_Description,
                Category = GeneralCategoryName,
                EnumProvider = "SdkSupportedTargetPlatformIdentifierEnumProvider",
                HelpUrl = "https://go.microsoft.com/fwlink/?linkid=2184943",
                DataSource = new()
                {
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                },
                Metadata = new()
                {
                    new() { Name = PropertyMetadata.SearchTerms, Value = PPR.ApplicationPage_TargetPlatformIdentifierProperty_SearchTerms },
                    new()
                    {
                        Name = PropertyMetadata.VisibilityCondition,
                        Value = @"
                          (and
                            (has-net-core-app-version-or-greater ""5.0"")
                            (not (has-evaluated-value ""Application"" ""TargetMultipleFrameworks"" true)))
                        "
                    }
                }
            };

            DynamicEnumProperty targetPlatformVersionProperty = new()
            {
                Name = "TargetPlatformVersion",
                DisplayName = PPR.ApplicationPage_TargetPlatformVersionProperty_DisplayName,
                Description = PPR.ApplicationPage_TargetPlatformVersionProperty_Description,
                Category = GeneralCategoryName,
                EnumProvider = "SdkSupportedTargetPlatformVersionEnumProvider",
                HelpUrl = "https://go.microsoft.com/fwlink/?linkid=2185153",
                DataSource = new()
                {
                    Persistence = "ProjectFileWithInterception",
                    HasConfigurationCondition = false
                },
                Metadata = new()
                {
                    new()
                    {
                        Name = PropertyMetadata.VisibilityCondition,
                        Value =
                        @"
                          (and
                            (has-net-core-app-version-or-greater ""5.0"")
                            (and
                              (ne (unevaluated ""Application"" ""TargetPlatformIdentifier"") """")
                              (not (has-evaluated-value ""Application"" ""TargetMultipleFrameworks"" true))))
                        "
                    },
                    new() { Name = PropertyMetadata.SearchTerms, Value = PPR.ApplicationPage_TargetPlatformVersionProperty_SearchTerms },
                    new() { Name = PropertyMetadata.DependsOn, Value = "Application::TargetPlatformIdentifier" }
                }
            };

            rule.Properties = new()
            {
                outputTypeProperty,
                targetMultipleFrameworksProperty,
                interceptedTargetFrameworkProperty,
                targetFrameworksProperty,
                installOtherFrameworksProperty,
                targetPlatformIdentifierProperty,
                targetPlatformVersionProperty,

            };

            rule.EndInit();

            List<Rule> rules = new();
            rules.Add(rule);

            return rules;
        }

        private static Category CreateCategory(string name, string displayName, string description)
        {
            Category category = new();
            category.BeginInit();
            category.Name = name;
            category.DisplayName = displayName;
            category.Description = description;
            category.EndInit();
            return category;
        }

        public IReadOnlyCollection<Rule> GetRules() => _rules.Value;
    }
}
