// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Rules;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
/// <para>
/// Provides the available severities for VB diagnostic sets.
/// </para>
/// <para>
/// When <c>TreatWarningsAsErrors</c> is off, a diagnostic set can be set to "None"
/// (i.e., disabled), "Warning", or "Error". When it is on, however, if a diagnostic
/// isn't disabled it is promoted to "Error", so the only choices are "None" and
/// "Error".
/// </para>
/// <para>
/// It is also possible that a diagnostic set is disabled entirely with no chance of it
/// being a Warning or Error. However, in this case we expect the UI control to be
/// disabled entirely so it doesn't matter what the supported values are, and the
/// extra complexity of handling that case serves no purpose.
/// </para>
/// </summary>
/// <remarks>
/// Makes use of the <see cref="TreatWarningsAsErrorsRuleProvider"/> to extract the
/// information it needs from the project file.
/// </remarks>
[ExportDynamicEnumValuesProvider(nameof(VBDiagnosticSeverityEnumProvider))]
[AppliesTo(ProjectCapability.VisualBasic)]
internal sealed class VBDiagnosticSeverityEnumProvider : SupportedValuesProvider
{
    private const string NoneValue = "None";
    private const string WarningValue = "Warning";
    private const string ErrorValue = "Error";

    private static readonly string[] s_ruleNames = new[] { TreatWarningsAsErrorsRuleProvider.RuleName };

    private static readonly List<IEnumValue> s_warningsAsErrorsOffEnumList = new()
    {
        new PageEnumValue(new EnumValue { Name = NoneValue, DisplayName = Resources.DiagnosticLevel_None }),
        new PageEnumValue(new EnumValue { Name = WarningValue, DisplayName = Resources.DiagnosticLevel_Warning }),
        new PageEnumValue(new EnumValue { Name = ErrorValue, DisplayName = Resources.DiagnosticLevel_Error })
    };

    private static readonly List<IEnumValue> s_warningsAsErrorsOnEnumList = new()
    {
        new PageEnumValue(new EnumValue { Name = NoneValue, DisplayName = Resources.DiagnosticLevel_None }),
        new PageEnumValue(new EnumValue { Name = ErrorValue, DisplayName = Resources.DiagnosticLevel_Error })
    };

    [ImportingConstructor]
    public VBDiagnosticSeverityEnumProvider(
        ConfiguredProject project,
        IProjectSubscriptionService subscriptionService)
        : base(project, subscriptionService)
    {
    }

    protected override string[] RuleNames => s_ruleNames;

    protected override int SortValues(IEnumValue a, IEnumValue b)
    {
        // This method will never be called, and the items are already sorted, anyway.
        throw new NotImplementedException();
    }

    protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
    {
        // This method will never be called, and we aren't converting MSBuild items to enum
        // values, anyway.
        throw new NotImplementedException();
    }

    protected override ICollection<IEnumValue> Transform(IProjectSubscriptionUpdate input)
    {
        IProjectRuleSnapshot snapshot = input.CurrentState[TreatWarningsAsErrorsRuleProvider.RuleName];

        string treatWarningsAsErrorsString = snapshot.Properties[TreatWarningsAsErrorsRuleProvider.TreatWarningsAsErrorsPropertyName];

        if (bool.TryParse(treatWarningsAsErrorsString, out bool treatWarningsAsErrors)
            && treatWarningsAsErrors)
        {
            return s_warningsAsErrorsOnEnumList;
        }

        return s_warningsAsErrorsOffEnumList;
    }
}
