// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class WarningLevelOverriddenValueProviderTests
{
    [Theory]
    [InlineData("", "False")]
    [InlineData("3.0", "False")]
    [InlineData("4.0", "False")]
    [InlineData("4.9", "False")]
    [InlineData("5.0", "True")]
    [InlineData("5.1", "True")]
    [InlineData("6.0", "True")]
    public async Task CheckValueAsync(string effectiveAnalysisLevel, string expectedWarningLevelOverriddenValue)
    {
        var provider = new WarningLevelOverriddenValueProvider();
        var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(
            WarningLevelOverriddenValueProvider.EffectiveAnalysisLevelPropertyName,
            effectiveAnalysisLevel);

        var unevaluatedResult = await provider.OnGetUnevaluatedPropertyValueAsync(
            WarningLevelOverriddenValueProvider.WarningLevelOverriddenPropertyName,
            string.Empty,
            defaultProperties);

        Assert.Equal(expectedWarningLevelOverriddenValue, actual: unevaluatedResult);

        var evaluatedResult = await provider.OnGetEvaluatedPropertyValueAsync(
            WarningLevelOverriddenValueProvider.WarningLevelOverriddenPropertyName,
            string.Empty,
            defaultProperties);

        Assert.Equal(expectedWarningLevelOverriddenValue, actual: evaluatedResult);
    }
}
