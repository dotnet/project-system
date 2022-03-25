// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
/// Provides a synthetic property indicating if the <c>WarningLevel</c> set by the
/// user (if any) will be overridden by the SDK.
/// </summary>
/// <remarks>
/// Based on the logic in https://github.com/dotnet/sdk/blob/a35fecf11708d1de6eb86f3dc3294f1644d3212e/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.Analyzers.targets.
/// To summarize: in the .NET 5.0 SDK and up, the <c>EffectiveAnalysisLevel</c>
/// property is computed from the <c>AnalysisLevel</c> property. If
/// <c>EffectiveAnalysisLevel</c> is >= 5.0, the SDK will override any previously-set
/// value of <c>WarningLevel</c> with its own.
/// </remarks>
[ExportInterceptingPropertyValueProvider(WarningLevelOverriddenPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal class WarningLevelOverriddenValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string WarningLevelOverriddenPropertyName = "WarningLevelOverridden";
    internal const string EffectiveAnalysisLevelPropertyName = "EffectiveAnalysisLevel";

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    private static async Task<string> OnGetPropertyValueAsync(IProjectProperties defaultProperties)
    {
        string effectiveAnalysisLevelString = await defaultProperties.GetEvaluatedPropertyValueAsync(EffectiveAnalysisLevelPropertyName);

        return
            (decimal.TryParse(effectiveAnalysisLevelString, out decimal effectiveAnalysisLevel)
             && effectiveAnalysisLevel >= 5.0m)
            ? bool.TrueString
            : bool.FalseString;
    }
}
