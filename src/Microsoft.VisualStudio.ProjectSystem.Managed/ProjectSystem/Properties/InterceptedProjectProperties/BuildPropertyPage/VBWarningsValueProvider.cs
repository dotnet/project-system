// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <remarks>
/// <para>
/// The VB "Compile" property page doesn't allow you to disable individual warnings
/// or promote individual warnings to errors. Instead, it groups related warnings
/// into sets and lets the user adjust the severity of the whole set at once. This
/// type is responsible for mapping between those sets and the actual property
/// values persisted to the project file.
/// </para>
/// <para>
/// Note this type stores IDs in instances of <see cref="ImmutableSortedSet{T}"/>,
/// rather a simple array or list. Two reasons:
/// <list type="number">
/// <item>We need to perform actual set operations (union, difference, etc.)</item>
/// <item>When writing IDs to the project file, we want to keep them sorted</item>
/// </list>
/// </para>
/// </remarks>
[ExportInterceptingPropertyValueProvider(
    new[]
    {
        ImplicitConversionPropertyName,
        LateBindingPropertyName,
        ImplicitTypePropertyName,
        UseOfVariablePriorToAssignmentPropertyName,
        ReturningRefTypeWithoutReturnValuePropertyName,
        ReturningIntrinsicValueTypeWithoutReturnValuePropertyName,
        UnusedLocalVariablePropertyName,
        InstanceVariableAccessesSharedMemberPropertyName,
        RecursiveOperatorOrPropertyAccessPropertyName,
        DuplicateOrOverlappingCatchBlocksPropertyName
    },
    ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class VBWarningsValueProvider : InterceptingPropertyValueProviderBase
{
    // The names of the property "sets" as known to the property page.
    internal const string ImplicitConversionPropertyName = "ImplicitConversion";
    internal const string LateBindingPropertyName = "LateBinding";
    internal const string ImplicitTypePropertyName = "ImplicitType";
    internal const string UseOfVariablePriorToAssignmentPropertyName = "UseOfVariablePriorToAssignment";
    internal const string ReturningRefTypeWithoutReturnValuePropertyName = "ReturningRefTypeWithoutReturnValue";
    internal const string ReturningIntrinsicValueTypeWithoutReturnValuePropertyName = "ReturningIntrinsicValueTypeWithoutReturnValue";
    internal const string UnusedLocalVariablePropertyName = "UnusedLocalVariable";
    internal const string InstanceVariableAccessesSharedMemberPropertyName = "InstanceVariableAccessesSharedMember";
    internal const string RecursiveOperatorOrPropertyAccessPropertyName = "RecursiveOperatorOrPropertyAccess";
    internal const string DuplicateOrOverlappingCatchBlocksPropertyName = "DuplicateOrOverlappingCatchBlocks";

    // The names of the properties persisted to the project file.
    internal const string OptionStrictPropertyName = "OptionStrict";
    internal const string WarningLevelPropertyName = "WarningLevel";
    internal const string TreatWarningsAsErrorsPropertyName = "TreatWarningsAsErrors";
    internal const string NoWarnPropertyName = "NoWarn";
    internal const string WarningsAsErrorsPropertyName = "WarningsAsErrors";

    // The possible values for any given set.
    internal const string NoneValue = "None";
    internal const string WarningValue = "Warning";
    internal const string ErrorValue = "Error";
    internal const string InconsistentValue = "";

    // The IDs associated with each set.
    private static readonly ImmutableDictionary<string, ImmutableSortedSet<int>> s_diagnosticIdMap = ImmutableDictionary<string, ImmutableSortedSet<int>>.Empty
        .Add(ImplicitConversionPropertyName,                            ImmutableSortedSet.Create(42016, 41999))
        .Add(LateBindingPropertyName,                                   ImmutableSortedSet.Create(42017, 42018, 42019, 42032, 42036))
        .Add(ImplicitTypePropertyName,                                  ImmutableSortedSet.Create(42020, 42021, 42022))
        .Add(UseOfVariablePriorToAssignmentPropertyName,                ImmutableSortedSet.Create(42104, 42108, 42109, 42030))
        .Add(ReturningRefTypeWithoutReturnValuePropertyName,            ImmutableSortedSet.Create(42105, 42106, 42107))
        .Add(ReturningIntrinsicValueTypeWithoutReturnValuePropertyName, ImmutableSortedSet.Create(42353, 42354, 42355))
        .Add(UnusedLocalVariablePropertyName,                           ImmutableSortedSet.Create(42024, 42099))
        .Add(InstanceVariableAccessesSharedMemberPropertyName,          ImmutableSortedSet.Create(42025))
        .Add(RecursiveOperatorOrPropertyAccessPropertyName,             ImmutableSortedSet.Create(41998, 42004, 42026))
        .Add(DuplicateOrOverlappingCatchBlocksPropertyName,             ImmutableSortedSet.Create(42029, 42031));

    private const string DiagnosticIdSeparator = ",";

    private static readonly string[] s_diagnosticIdSeparators = new[] { DiagnosticIdSeparator };

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        ImmutableSortedSet<int> originalWarningsAsErrorIds = await GetWarningsAsErrorsIdsAsync(defaultProperties);
        ImmutableSortedSet<int> originalNoWarnIds = await GetNoWarnIdsAsync(defaultProperties);
        ImmutableSortedSet<int>? updatedWarningsAsErrorIds = null;
        ImmutableSortedSet<int>? updatedNoWarnIds = null;

        ImmutableSortedSet<int> idsInSet = s_diagnosticIdMap[propertyName];

        switch (unevaluatedPropertyValue)
        {
            case NoneValue:
            {
                // Remove from WarningsAsErrors; add to NoWarn.
                updatedWarningsAsErrorIds = originalWarningsAsErrorIds.Except(idsInSet);
                updatedNoWarnIds = originalNoWarnIds.Union(idsInSet);
                break;
            }

            case WarningValue:
            {
                // Remove from both WarningsAsErrors and NoWarn.
                updatedWarningsAsErrorIds = originalWarningsAsErrorIds.Except(idsInSet);
                updatedNoWarnIds = originalNoWarnIds.Except(idsInSet);
                break;
            }

            case ErrorValue:
            {
                // Remove from NoWarn; add to WarningsAsErrors.
                updatedWarningsAsErrorIds = originalWarningsAsErrorIds.Union(idsInSet);
                updatedNoWarnIds = originalNoWarnIds.Except(idsInSet);
                break;
            }

            default:
                break;
        }

        if (updatedWarningsAsErrorIds is not null
            && updatedWarningsAsErrorIds != originalWarningsAsErrorIds)
        {
            await SetWarningsAsErrorIdsAsync(updatedWarningsAsErrorIds, defaultProperties, dimensionalConditions);
        }

        if (updatedNoWarnIds is not null
            && updatedNoWarnIds != originalNoWarnIds)
        {
            await SetNoWarnIdsAsync(updatedNoWarnIds, defaultProperties, dimensionalConditions);
        }

        return null;
    }

    private static async Task<string> OnGetPropertyValueAsync(string propertyName, IProjectProperties projectProperties)
    {
        // <OptionStrict>On</OptionsStrict> forces certain sets of diagnostics to be errors.
        if (propertyName is ImplicitConversionPropertyName or LateBindingPropertyName or ImplicitTypePropertyName
            && await IsOptionStrictOnAsync(projectProperties))
        {
            return ErrorValue;
        }

        // If all warnings are disabled, then this set is disabled.
        if (await IsDisableAllWarningsOnAsync(projectProperties))
        {
            return NoneValue;
        }

        ImmutableSortedSet<int> noWarnIdsSet = await GetNoWarnIdsAsync(projectProperties);
        NumbersInSetResult idsInNoWarnIdsSet = NumbersInSet(noWarnIdsSet, s_diagnosticIdMap[propertyName]);

        // If all active warnings are promoted to errors, and these aren't disabled, they must be errors.
        if (await IsTreatAllWarningsAsErrorsOnAsync(projectProperties)
            && idsInNoWarnIdsSet == NumbersInSetResult.None)
        {
            return ErrorValue;
        }

        // If all the diagnostics in the set are disabled, then the set is disabled.
        if (idsInNoWarnIdsSet == NumbersInSetResult.All)
        {
            return NoneValue;
        }

        ImmutableSortedSet<int> warningsAsErrorsIdsSet = await GetWarningsAsErrorsIdsAsync(projectProperties);
        NumbersInSetResult idsInWarningsAsErrorsIdsSet = NumbersInSet(warningsAsErrorsIdsSet, s_diagnosticIdMap[propertyName]);

        // If all the diagnostics in the set are promoted to errors, and none are disabled, then the set is promoted to an error.
        if (idsInWarningsAsErrorsIdsSet == NumbersInSetResult.All
            && idsInNoWarnIdsSet == NumbersInSetResult.None)
        {
            return ErrorValue;
        }

        // If none of the diagnostics are disabled and none are promoted to errors, then they are all warnings.
        if (idsInNoWarnIdsSet == NumbersInSetResult.None
            && idsInWarningsAsErrorsIdsSet == NumbersInSetResult.None)
        {
            return WarningValue;
        }

        // The state of the diagnostics in the set is inconsistent.
        return InconsistentValue;
    }

    private static async Task SetNoWarnIdsAsync(ImmutableSortedSet<int> updatedNoWarnIds, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions)
    {
        string value = CreateIdList(updatedNoWarnIds);

        await defaultProperties.SetPropertyValueAsync(NoWarnPropertyName, value, dimensionalConditions);
    }

    private static async Task SetWarningsAsErrorIdsAsync(ImmutableSortedSet<int> updatedWarningsAsErrorIds, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions)
    {
        string value = CreateIdList(updatedWarningsAsErrorIds);

        await defaultProperties.SetPropertyValueAsync(WarningsAsErrorsPropertyName, value, dimensionalConditions);
    }

    private static async Task<ImmutableSortedSet<int>> GetNoWarnIdsAsync(IProjectProperties projectProperties)
    {
        string noWarnValue = await projectProperties.GetEvaluatedPropertyValueAsync(NoWarnPropertyName);

        return ParseIdList(noWarnValue);
    }

    private static ImmutableSortedSet<int> ParseIdList(string idList)
    {
        ImmutableSortedSet<int> ids = ImmutableSortedSet<int>.Empty;
        string[] idsAsStrings = idList.Split(s_diagnosticIdSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string idString in idsAsStrings)
        {
            if (int.TryParse(idString, out int id))
            {
                ids = ids.Add(id);
            }
        }

        return ids;
    }

    private static string CreateIdList(ImmutableSortedSet<int> idSet)
    {
        return string.Join(DiagnosticIdSeparator, idSet);
    }

    private static async Task<ImmutableSortedSet<int>> GetWarningsAsErrorsIdsAsync(IProjectProperties projectProperties)
    {
        string warningsAsErrorsValue = await projectProperties.GetEvaluatedPropertyValueAsync(WarningsAsErrorsPropertyName);

        return ParseIdList(warningsAsErrorsValue);
    }

    private static async Task<bool> IsOptionStrictOnAsync(IProjectProperties projectProperties)
    {
        string optionStrictValue = await projectProperties.GetEvaluatedPropertyValueAsync(OptionStrictPropertyName);
        bool optionStrictOn = StringComparers.PropertyLiteralValues.Equals(optionStrictValue, "On");

        return optionStrictOn;
    }

    private static async Task<bool> IsDisableAllWarningsOnAsync(IProjectProperties projectProperties)
    {
        string warningLevelValue = await projectProperties.GetEvaluatedPropertyValueAsync(WarningLevelPropertyName);
        bool disableAllWarningsOn = StringComparers.PropertyLiteralValues.Equals(warningLevelValue, "0");

        return disableAllWarningsOn;
    }

    private static async Task<bool> IsTreatAllWarningsAsErrorsOnAsync(IProjectProperties projectProperties)
    {
        string treatAllWarningsAsErrorsValue = await projectProperties.GetEvaluatedPropertyValueAsync(TreatWarningsAsErrorsPropertyName);
        bool treatAllWarningsAsErrorsOn = StringComparers.PropertyLiteralValues.Equals(treatAllWarningsAsErrorsValue, "true");

        return treatAllWarningsAsErrorsOn;
    }

    private enum NumbersInSetResult
    {
        None,
        Some,
        All
    }

    private static NumbersInSetResult NumbersInSet(ImmutableSortedSet<int> set, ImmutableSortedSet<int> numbersToCheck)
    {
        int numbersFound = set.Intersect(numbersToCheck).Count;

        if (numbersFound == numbersToCheck.Count)
        {
            return NumbersInSetResult.All;
        }
        else if (numbersFound == 0)
        {
            return NumbersInSetResult.None;
        }
        else
        {
            return NumbersInSetResult.Some;
        }
    }
}
