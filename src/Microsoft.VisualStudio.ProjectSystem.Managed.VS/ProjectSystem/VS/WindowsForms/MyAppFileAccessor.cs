// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Design.Serialization;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

[Export(typeof(IMyAppFileAccessor))]
[AppliesTo(ProjectCapability.DotNet)]
internal class MyAppFileAccessor : IMyAppFileAccessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _filePath;
    private readonly string _fileName = "Application.myapp";

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

    public MyAppFileAccessor(IServiceProvider serviceProvider, string filePath, string fileName)
    {
        _serviceProvider = serviceProvider;
        _filePath = filePath;
        _fileName = fileName;
    }

    public async Task<bool?> GetMySubMainAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, MySubMainProperty);

        if (value is null)
            return null;
        else
            return bool.Parse(value);
    }
    
    public async Task SetMySubMainAsync(string mySubMain)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(MySubMainProperty, mySubMain);
        }
    }

    public async Task<string?> GetMainFormAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, MainFormProperty);

        if (value is null)
            return null;
        else
            return value;
    }
    
    public async Task SetMainFormAsync(string mainForm)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(MySubMainProperty, mainForm);
        }
    }

    public async Task<bool?> GetSingleInstanceAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, SingleInstanceProperty);

        if (value is null)
            return null;
        else
            return bool.Parse(value);
    }

    public async Task SetSingleInstanceAsync(bool singleInstance)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(SingleInstanceProperty, singleInstance.ToString());
        }
    }
    
    public async Task<int?> GetShutdownModeAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, ShutdownModeProperty);

        if (value is null)
            return null;
        else
            return int.Parse(value);
    }

    public async Task SetShutdownModeAsync(int shutdownMode)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(ShutdownModeProperty, shutdownMode.ToString());
        }
    }

    public async Task<bool?> GetEnableVisualStylesAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, EnableVisualStylesProperty);

        if (value is null)
            return null;
        else
            return bool.Parse(value);
    }

    public async Task SetEnableVisualStylesAsync(bool enableVisualStyles)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(EnableVisualStylesProperty, enableVisualStyles.ToString());
        }
    }

    public async Task<int?> GetAuthenticationModeAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, AuthenticationModeProperty);

        if (value is null)
            return null;
        else
            return int.Parse(value);
    }

    public async Task SetAuthenticationModeAsync(int authenticationMode)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(AuthenticationModeProperty, authenticationMode.ToString());
        }
    }

    public async Task<bool?> GetSaveMySettingsOnExitAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, SaveMySettingsOnExitProperty);

        if (value is null)
            return null;
        else
            return bool.Parse(value);
    }

    public async Task SetSaveMySettingsOnExitAsync(bool saveMySettingsOnExit)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(SaveMySettingsOnExitProperty, saveMySettingsOnExit.ToString());
        }
    }

    public async Task<int?> GetHighDpiModeAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, HighDpiModeProperty);

        if (value is null)
            return null;
        else
            return int.Parse(value);
    }

    public async Task SetHighDpiModeAsync(int highDpiMode)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(HighDpiModeProperty, highDpiMode.ToString());
        }
    }

    public async Task<string?> GetSplashScreenAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, SplashScreenProperty);

        if (value is null)
            return null;
        else
            return value;
    }

    public async Task SetSplashScreenAsync(string splashScreen)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(SplashScreenProperty, splashScreen);
        }
    }

    public async Task<int?> GetMinimumSplashScreenDisplayTimeAsync()
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(false);
        string? value = myAppDocument?.GetProperty(_filePath, MinimumSplashScreenDisplayTimeProperty);

        if (value is null)
            return null;
        else
            return int.Parse(value);
    }

    public async Task SetMinimumSplashScreenDisplayTimeAsync(int minimumSplashScreenDisplayTime)
    {
        MyAppDocument? myAppDocument = await TryGetMyAppFileAsync(true);

        if (myAppDocument is not null)
        {
            myAppDocument.SetProperty(MinimumSplashScreenDisplayTimeProperty, minimumSplashScreenDisplayTime.ToString());
        }
    }

    private async Task<MyAppDocument?> TryGetMyAppFileAsync(bool create)
    {
        string _filePath = Path.GetFullPath(_fileName);

        if (_filePath is null)
        {
            throw new InvalidOperationException($"The file {_fileName} path cannot be found.");
        }

        var docData = new DocData(_serviceProvider, _filePath);

        if (docData is not null)
        {
            var textReader = new DocDataTextReader(docData);
            return new MyAppDocument(_fileName, textReader);
        }

        return null;
    }
}
