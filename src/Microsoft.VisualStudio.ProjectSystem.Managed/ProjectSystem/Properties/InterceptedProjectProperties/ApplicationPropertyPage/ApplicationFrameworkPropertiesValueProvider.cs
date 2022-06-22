// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(
    new[] 
    {
        EnableVisualStylesProperty,
        SingleInstanceProperty,
        SaveMySettingsOnExitProperty,
        HighDpiModeProperty,
        AuthenticationModeProperty,
        ShutdownModeProperty,
        SplashScreenProperty,
        MinimumSplashScreenDisplayTimeProperty
    }, 
    ExportInterceptingPropertyValueProviderFile.ProjectFile)] //Note: we don't need to save it in the project file, but we'll intercept the properties.
internal sealed class ApplicationFrameworkPropertiesValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string EnableVisualStylesProperty = "EnableVisualStyles";
    internal const string SingleInstanceProperty = "SingleInstance";
    internal const string SaveMySettingsOnExitProperty = "SaveMySettingsOnExit";
    internal const string HighDpiModeProperty = "HighDpiMode";
    internal const string AuthenticationModeProperty = "VBAuthenticationMode";
    internal const string ShutdownModeProperty = "ShutdownMode";
    internal const string SplashScreenProperty = "SplashScreen";
    internal const string MinimumSplashScreenDisplayTimeProperty = "MinimumSplashScreenDisplayTime";

    private readonly IMyAppFileAccessor _myAppXamlFileAccessor;

    [ImportingConstructor]
    public ApplicationFrameworkPropertiesValueProvider(IMyAppFileAccessor MyAppXamlFileAccessor)
    {
        _myAppXamlFileAccessor = MyAppXamlFileAccessor;
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        // ValueProvider needs to convert string enums to valid values to be saved.
        if (propertyName == AuthenticationModeProperty)
        {
            if (unevaluatedPropertyValue == "Windows")
                unevaluatedPropertyValue = "0";

            if (unevaluatedPropertyValue == "ApplicationDefined")
                unevaluatedPropertyValue= "1";
        }
        else if (propertyName == ShutdownModeProperty)
        {
            if (unevaluatedPropertyValue == "AfterMainFormCloses")
                unevaluatedPropertyValue = "0";

            if (unevaluatedPropertyValue == "AfterAllFormsClose")
                unevaluatedPropertyValue = "1";
        }

        await (propertyName switch
        {
            //ApplicationFrameworkProperty => _myAppXamlFileAccessor.SetMySubMainAsync(unevaluatedPropertyValue),
            EnableVisualStylesProperty => _myAppXamlFileAccessor.SetEnableVisualStylesAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
            SingleInstanceProperty => _myAppXamlFileAccessor.SetSingleInstanceAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
            SaveMySettingsOnExitProperty => _myAppXamlFileAccessor.SetSaveMySettingsOnExitAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
            HighDpiModeProperty => _myAppXamlFileAccessor.SetHighDpiModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            AuthenticationModeProperty => _myAppXamlFileAccessor.SetAuthenticationModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            ShutdownModeProperty => _myAppXamlFileAccessor.SetShutdownModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            SplashScreenProperty => _myAppXamlFileAccessor.SetSplashScreenAsync(unevaluatedPropertyValue),
            MinimumSplashScreenDisplayTimeProperty => _myAppXamlFileAccessor.SetMinimumSplashScreenDisplayTimeAsync(Convert.ToInt16(unevaluatedPropertyValue)),

            _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
        });

        return null;
    }

    private async Task<string> GetPropertyValueAsync(string propertyName)
    {
        return propertyName switch
        {
            //ApplicationFrameworkProperty => (await _myAppXamlFileAccessor.GetMySubMainAsync()).ToString() ?? string.Empty,
            EnableVisualStylesProperty => (await _myAppXamlFileAccessor.GetEnableVisualStylesAsync()).ToString() ?? string.Empty,
            SingleInstanceProperty => (await _myAppXamlFileAccessor.GetSingleInstanceAsync()).ToString() ?? string.Empty,
            SaveMySettingsOnExitProperty => (await _myAppXamlFileAccessor.GetSaveMySettingsOnExitAsync()).ToString() ?? string.Empty,
            HighDpiModeProperty => (await _myAppXamlFileAccessor.GetHighDpiModeAsync()).ToString() ?? string.Empty,
            AuthenticationModeProperty => (await _myAppXamlFileAccessor.GetAuthenticationModeAsync()).ToString() ?? string.Empty,
            ShutdownModeProperty => (await _myAppXamlFileAccessor.GetShutdownModeAsync()).ToString() ?? string.Empty,
            SplashScreenProperty => await _myAppXamlFileAccessor.GetSplashScreenAsync() ?? string.Empty,
            MinimumSplashScreenDisplayTimeProperty => (await _myAppXamlFileAccessor.GetMinimumSplashScreenDisplayTimeAsync()).ToString() ?? string.Empty,

            _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
        };
    }
}
