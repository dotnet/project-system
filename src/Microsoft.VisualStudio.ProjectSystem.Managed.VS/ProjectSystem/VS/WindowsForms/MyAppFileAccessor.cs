// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

[Export(typeof(IMyAppFileAccessor))]
[AppliesTo(ProjectCapability.DotNet)]
internal class MyAppFileAccessor : IMyAppFileAccessor, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly UnconfiguredProject _project;
    private readonly IProjectThreadingService _threadingService;
    private DocData? _docData;
    private MyAppDocument? myAppDocument;

    private const string MySubMainProperty = "MySubMain";
    private const string MainFormProperty = "MainForm";
    private const string SingleInstanceProperty = "SingleInstance";
    private const string ShutdownModeProperty = "ShutdownMode";
    private const string EnableVisualStylesProperty = "EnableVisualStyles";
    private const string AuthenticationModeProperty = "AuthenticationMode";
    private const string SaveMySettingsOnExitProperty = "SaveMySettingsOnExit";
    private const string HighDpiModeProperty = "HighDpiMode";
    private const string SplashScreenProperty = "SplashScreen";
    private const string MinimumSplashScreenDisplayTimeProperty = "MinimumSplashScreenDisplayTime";

    [ImportingConstructor]
    public MyAppFileAccessor([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, UnconfiguredProject project, IProjectThreadingService threadingService)
    {
        _serviceProvider = serviceProvider;
        _project = project;
        _threadingService = threadingService;
    }

    public void Dispose()
    {
        _docData?.Dispose();
    }

    private void DocData_Modifying(object sender, EventArgs e)
    {
        myAppDocument = null;
    }

    private async Task<MyAppDocument?> TryGetMyAppFileAsync()
    {
        if (_docData is null)
        {
            string absolutePath = _project.MakeRooted("My Project\\Application.myapp");

            await _threadingService.SwitchToUIThread();
            try
            {
                _docData = new DocData(_serviceProvider, absolutePath);
                _docData.Modifying += DocData_Modifying;
            } 
            catch (Exception)
            {
                // If we have reached here, it means that the file doesn't exist in that location.
                return null;
            }
            
            await TaskScheduler.Default;
        }

        if (myAppDocument is null)
        {
            myAppDocument = new MyAppDocument(_docData);
        }

        return myAppDocument;
    }

    private async Task SetPropertyAsync(string propertyName, string value)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync();

        if (myAppDocument is not null)
        {
            await _threadingService.SwitchToUIThread();
            myAppDocument.SetProperty(propertyName, value);
        }
    }

    private async Task<string?> GetStringPropertyValueAsync(string propertyName)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync();
        return myAppDocument?.GetProperty(propertyName);
    }

    private async Task<bool?> GetBooleanPropertyValueAsync(string propertyName)
    {
        string? value = await GetStringPropertyValueAsync(propertyName);

        if (bool.TryParse(value, out bool booleanValue))
            return booleanValue;
        return null;
    }

    private async Task<int?> GetIntPropertyValueAsync(string propertyName)
    {
        string? value = await GetStringPropertyValueAsync(propertyName);

        if (int.TryParse(value, out int intValue))
            return intValue;
        return null;
    }

    public async Task<bool?> GetMySubMainAsync() => await GetBooleanPropertyValueAsync(MySubMainProperty);

    public async Task SetMySubMainAsync(string value) => await SetPropertyAsync(MySubMainProperty, value);

    public async Task<string?> GetMainFormAsync() => await GetStringPropertyValueAsync(MainFormProperty);

    public async Task SetMainFormAsync(string value) => await SetPropertyAsync(MainFormProperty, value);

    public async Task<bool?> GetSingleInstanceAsync() => await GetBooleanPropertyValueAsync(SingleInstanceProperty);

    public async Task SetSingleInstanceAsync(bool value) => await SetPropertyAsync(SingleInstanceProperty, value.ToString().ToLower());

    public async Task<int?> GetShutdownModeAsync() => await GetIntPropertyValueAsync(ShutdownModeProperty);

    public async Task SetShutdownModeAsync(int value) => await SetPropertyAsync(ShutdownModeProperty, value.ToString());

    public async Task<bool?> GetEnableVisualStylesAsync() => await GetBooleanPropertyValueAsync(EnableVisualStylesProperty);

    public async Task SetEnableVisualStylesAsync(bool value) => await SetPropertyAsync(EnableVisualStylesProperty, value.ToString().ToLower());

    public async Task<int?> GetAuthenticationModeAsync() => await GetIntPropertyValueAsync(AuthenticationModeProperty);

    public async Task SetAuthenticationModeAsync(int value) => await SetPropertyAsync(AuthenticationModeProperty, value.ToString());

    public async Task<bool?> GetSaveMySettingsOnExitAsync() => await GetBooleanPropertyValueAsync(SaveMySettingsOnExitProperty);

    public async Task SetSaveMySettingsOnExitAsync(bool value) => await SetPropertyAsync(SaveMySettingsOnExitProperty, value.ToString().ToLower());

    public async Task<int?> GetHighDpiModeAsync() => await GetIntPropertyValueAsync(HighDpiModeProperty);

    public async Task SetHighDpiModeAsync(int value) => await SetPropertyAsync(HighDpiModeProperty, value.ToString());

    public async Task<string?> GetSplashScreenAsync() => await GetStringPropertyValueAsync(SplashScreenProperty);

    public async Task SetSplashScreenAsync(string value) => await SetPropertyAsync(SplashScreenProperty, value);

    public async Task<int?> GetMinimumSplashScreenDisplayTimeAsync() => await GetIntPropertyValueAsync(MinimumSplashScreenDisplayTimeProperty);

    public async Task SetMinimumSplashScreenDisplayTimeAsync(int value) => await SetPropertyAsync(MinimumSplashScreenDisplayTimeProperty, value.ToString());

}
