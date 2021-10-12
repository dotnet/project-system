// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Rules.PropertyPages
{
    /// <remarks>
    /// We want to provide certain rules when the "MyCapability" capability is present, but remove them if it goes away.
    /// The <see cref="IProjectDynamicLoadComponent"/> is designed specifically for these situations.
    /// </remarks>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo("MyCapability")]
    internal class PropertyPageRuleProvider : IProjectDynamicLoadComponent
    {
        /// <summary>
        /// A CPS-provided service that supports adding and removing in-memory <see cref="Rule"/>s.
        /// </summary>
        private readonly IAdditionalRuleDefinitionsService _additionalRuleDefinitionsService;
        /// <summary>
        /// Use a <see cref="Lazy{T}"/> so we don't create the <see cref="Rule"/> until we need it.
        /// Technically this is unnecessary, since the constructor won't be called until the first
        /// time the capabilities are satisified, and <see cref="LoadAsync"/> will be called shortly
        /// after that. As such, making this lazy really just makes it very clear that it definitely
        /// won't be created until it is needed, and will only be created once.
        /// </summary>
        private readonly Lazy<Rule> _rule;

        [ImportingConstructor]
        public PropertyPageRuleProvider(
            IAdditionalRuleDefinitionsService additionalRuleDefinitionsService)
        {
            _additionalRuleDefinitionsService = additionalRuleDefinitionsService;
            _rule = new Lazy<Rule>(CreateRule);
        }

        /// <summary>
        /// Called when the specified capabilities are satisfied.
        /// We can add our rules.
        /// </summary>
        public Task LoadAsync()
        {
            _additionalRuleDefinitionsService.AddRuleDefinition(_rule.Value, context: "Project");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the specified capabilities are no longer satisfied.
        /// We can remove our rules.
        /// </summary>
        public Task UnloadAsync()
        {
            _additionalRuleDefinitionsService.RemoveRuleDefinition(_rule.Value);

            return Task.CompletedTask;
        }

        private Rule CreateRule()
        {
            Rule rule = new();
            
            // Rule implements ISupportInitialize; we need to be sure to call BeginInit and EndInit.
            rule.BeginInit();

            // Basic information

            rule.Name = "MyInMemoryPropertyPage";
            rule.Description = "A Rule that is defined in memory and dynamically loaded and unloaded";
            rule.DisplayName = Resources.MyPropertyPage_DisplayName;
            rule.PageTemplate = "generic";
            rule.Order = 10;

            // Metadata

            // (no metadata)

            // Categories

            rule.Categories.Add(new Category { Name = "General", DisplayName = "General" });
            rule.Categories.Add(new Category { Name = "OtherSettings", DisplayName = "Other Settings" });

            // Data source

            rule.DataSource = new DataSource
            {
                Persistence = "ProjectFile",
                SourceOfDefaultValue = DefaultValueSourceLocation.AfterContext,
                HasConfigurationCondition = false,
            };

            // Properties

            rule.Properties.Add(new StringProperty
            {
                Name = "MyProperty",
                DisplayName = "My Property",
                Description = Resources.MyPropertyPage_PropertyDescription,
                Category = "OtherSettings"
            });

            rule.EndInit();

            return rule;
        }
    }
}
