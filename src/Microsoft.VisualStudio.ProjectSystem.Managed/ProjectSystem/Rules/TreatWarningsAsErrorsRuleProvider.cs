// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Rules;

/// <summary>
/// Defines a <see cref="Rule"/> to extract information to support the <see cref="VBDiagnosticSeverityEnumProvider"/>.
/// </summary>
[ExportRuleObjectProvider(nameof(TreatWarningsAsErrorsRuleProvider), PropertyPageContexts.Project)]
[AppliesTo(ProjectCapability.VisualBasic)]
[Order(0)]
internal sealed class TreatWarningsAsErrorsRuleProvider : IRuleObjectProvider
{
    internal const string RuleName = "TreatWarningsAsErrorsRule";
    internal const string TreatWarningsAsErrorsPropertyName = "TreatWarningsAsErrors";

    private readonly Lazy<List<Rule>> _rules = new(CreateRules);

    private static List<Rule> CreateRules()
    {
        Rule rule = new Rule();

        rule.BeginInit();

        rule.Name = RuleName;
        rule.PageTemplate = "generic";

        rule.DataSource = new DataSource
        {
            Persistence = "ProjectFile",
            HasConfigurationCondition = true,
            SourceOfDefaultValue = DefaultValueSourceLocation.AfterContext
        };

        rule.Properties.Add(new BoolProperty
        {
            Name = TreatWarningsAsErrorsPropertyName,
            ReadOnly = true,
            Visible = false
        });

        rule.EndInit();

        List<Rule> rules = new(capacity: 1) { rule };

        return rules;
    }

    public IReadOnlyCollection<Rule> GetRules()
    {
        return _rules.Value;
    }
}
