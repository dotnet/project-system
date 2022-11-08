// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class VBWarningsValueProviderTests
{
    [Theory]                         // Property                                          Option  Warning  All as   NoWarn         Warnings as    Expected
                                     //                                                   Strict  Level    Errors                  Errors
    [InlineData(VBWarningsValueProvider.ImplicitConversionPropertyName,                   "On",   "0",     "",      "",            "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.ImplicitConversionPropertyName,                   "On",   "0",     "false", "",            "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.ImplicitConversionPropertyName,                   "On",   "0",     "",      "42016,41999", "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.LateBindingPropertyName,                          "On",   "0",     "",      "",            "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.ImplicitTypePropertyName,                         "On",   "0",     "",      "",            "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "On",   "1",     "",      "",            "",            VBWarningsValueProvider.WarningValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "0",     "",      "",            "",            VBWarningsValueProvider.NoneValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "",      "",            "",            VBWarningsValueProvider.WarningValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "0",     "true",  "",            "",            VBWarningsValueProvider.NoneValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "true",  "",            "",            VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "true",  "42024,42099", "",            VBWarningsValueProvider.NoneValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "",      "42024,42099", "",            VBWarningsValueProvider.NoneValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "",      "",            "42024,42099", VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.UnusedLocalVariablePropertyName,                  "",     "1",     "",      "42024",       "42099",       VBWarningsValueProvider.InconsistentValue)]
    [InlineData(VBWarningsValueProvider.InstanceVariableAccessesSharedMemberPropertyName, "",     "1",     "",      "42025",       "",            VBWarningsValueProvider.NoneValue)]
    [InlineData(VBWarningsValueProvider.InstanceVariableAccessesSharedMemberPropertyName, "",     "1",     "",      "",            "42025",       VBWarningsValueProvider.ErrorValue)]
    [InlineData(VBWarningsValueProvider.InstanceVariableAccessesSharedMemberPropertyName, "",     "1",     "",      "",            "",            VBWarningsValueProvider.WarningValue)]
    public async Task GetPropertyValueTests(string propertyName, string optionStrict, string warningLevel, string treatWarningsAsErrors, string noWarn, string specificWarningsAsErrors, string expectedValue)
    {
        var provider = new VBWarningsValueProvider();

        var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>()
        {
            { VBWarningsValueProvider.OptionStrictPropertyName, optionStrict },
            { VBWarningsValueProvider.WarningLevelPropertyName, warningLevel  },
            { VBWarningsValueProvider.TreatWarningsAsErrorsPropertyName, treatWarningsAsErrors },
            { VBWarningsValueProvider.NoWarnPropertyName, noWarn },
            { VBWarningsValueProvider.WarningsAsErrorsPropertyName, specificWarningsAsErrors }
        });

        var result = await provider.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue: "", defaultProperties);

        Assert.Equal(expectedValue, result);
    }
}
