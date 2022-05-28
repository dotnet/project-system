// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(new[] {
    ApplicationFrameworkProperty,
    EnableVisualStylesProperty,
    SingleInstanceProperty,
    SaveMySettingsOnExitProperty,
    HighDpiModeProperty,
    AuthenticationModeProperty,
    ShutdownModeProperty,
    SplashScreenProperty,
    MinimumSplashScreenDisplayTimeProperty}, ExportInterceptingPropertyValueProviderFile.ProjectFile)] //Note: we don't need to save it in the project file, but we'll intercept the properties.
internal class ApplicationFrameworkPropertiesValueProvider : InterceptingPropertyValueProviderBase
{
    private const string ApplicationFrameworkProperty = "UseApplicationFramework"; //TODO: this is saved as MySubMain
    private const string EnableVisualStylesProperty = "EnableVisualStyles";
    private const string SingleInstanceProperty = "SingleInstance";
    private const string SaveMySettingsOnExitProperty = "SaveMySettingsOnExit";
    private const string HighDpiModeProperty = "HighDpiMode";
    private const string AuthenticationModeProperty = "AuthenticationMode";
    private const string ShutdownModeProperty = "ShutdownMode";
    private const string SplashScreenProperty = "SplashScreen";
    private const string MinimumSplashScreenDisplayTimeProperty = "MinimumSplashScreenDisplayTime";

    private readonly IMyAppXamlFileAccessor _myAppXamlFileAccessor;

    [ImportingConstructor]
    public ApplicationFrameworkPropertiesValueProvider(IMyAppXamlFileAccessor MyAppXamlFileAccessor)
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

    public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        return propertyName switch
        {
            ApplicationFrameworkProperty => _myAppXamlFileAccessor.SetMySubMainAsync(unevaluatedPropertyValue),
            SingleInstanceProperty => _myAppXamlFileAccessor.SetSingleInstanceAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
            SaveMySettingsOnExitProperty => _myAppXamlFileAccessor.SetSaveMySettingsOnExitAsync(Convert.ToBoolean(unevaluatedPropertyValue)),
            HighDpiModeProperty => _myAppXamlFileAccessor.SetHighDpiModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            AuthenticationModeProperty => _myAppXamlFileAccessor.SetAuthenticationModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            ShutdownModeProperty => _myAppXamlFileAccessor.SetShutdownModeAsync(Convert.ToInt16(unevaluatedPropertyValue)),
            SplashScreenProperty => _myAppXamlFileAccessor.SetSplashScreenAsync(unevaluatedPropertyValue),
            MinimumSplashScreenDisplayTimeProperty => _myAppXamlFileAccessor.SetMinimumSplashScreenDisplayTimeAsync(Convert.ToInt16(unevaluatedPropertyValue)),

            _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
        };
    }
    
    private async Task<string> GetPropertyValueAsync(string propertyName)
    {
        return propertyName switch
        {
            ApplicationFrameworkProperty => (await _myAppXamlFileAccessor.GetMySubMainAsync()).ToString(),
            EnableVisualStylesProperty => (await _myAppXamlFileAccessor.GetEnableVisualStylesAsync()).ToString(),
            SingleInstanceProperty => (await _myAppXamlFileAccessor.GetSingleInstanceAsync()).ToString(),
            SaveMySettingsOnExitProperty => (await _myAppXamlFileAccessor.GetSaveMySettingsOnExitAsync()).ToString(),
            HighDpiModeProperty => (await _myAppXamlFileAccessor.GetHighDpiModeAsync()).ToString(),
            AuthenticationModeProperty => (await _myAppXamlFileAccessor.GetAuthenticationModeAsync()).ToString(),
            ShutdownModeProperty => (await _myAppXamlFileAccessor.GetShutdownModeAsync()).ToString(),
            SplashScreenProperty => await _myAppXamlFileAccessor.GetSplashScreenAsync(),
            MinimumSplashScreenDisplayTimeProperty => (await _myAppXamlFileAccessor.GetMinimumSplashScreenDisplayTimeAsync()).ToString(),

            _ => throw new InvalidOperationException($"The provider does not support the '{propertyName}' property.")
        };
    }
}
