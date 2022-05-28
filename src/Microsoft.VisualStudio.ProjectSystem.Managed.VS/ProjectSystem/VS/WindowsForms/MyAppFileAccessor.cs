// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

[Export(typeof(IMyAppXamlFileAccessor))]
[AppliesTo(ProjectCapabilities.VB)]
internal class MyAppFileAccessor : IMyAppXamlFileAccessor
{
    private readonly UnconfiguredProject _project;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVsUIService<IVsRunningDocumentTable> _runningDocumentTable;
    private readonly IProjectThreadingService _threadingService;

    private readonly MyAppDocument _myappDocument;

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
    public MyAppFileAccessor(
        UnconfiguredProject project,
        IServiceProvider serviceProvider,
        IVsUIService<IVsRunningDocumentTable> runningDocumnetTable,
        IProjectThreadingService threadingService)
    {
        _project = project;
        _serviceProvider = serviceProvider;
        _runningDocumentTable = runningDocumnetTable;
        _threadingService = threadingService;

        _myappDocument = new MyAppDocument("Application.myapp", "filePath"); //TODO: get actual file name and file path.
    }
    public async Task<bool> GetMySubMainAsync()
    {
        await _threadingService.SwitchToUIThread();

        return bool.Parse(_myappDocument.GetProperty(MySubMainProperty));
    }
    
    public async Task SetMySubMainAsync(string mySubMain)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(MySubMainProperty, mySubMain);
    }

    public async Task<string> GetMainFormAsync()
    {
        await _threadingService.SwitchToUIThread();

        return _myappDocument.GetProperty(MainFormProperty);
    }
    
    public async Task SetMainFormAsync(string mainForm)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(MainFormProperty, mainForm);
    }

    public async Task<bool> GetSingleInstanceAsync()
    {
        await _threadingService.SwitchToUIThread();
        return bool.Parse(_myappDocument.GetProperty(SingleInstanceProperty));
    }

    public async Task SetSingleInstanceAsync(bool singleInstance)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(SingleInstanceProperty, singleInstance.ToString());
    }
    
    public async Task<int> GetShutdownModeAsync()
    {
        await _threadingService.SwitchToUIThread();

        return int.Parse(_myappDocument.GetProperty(ShutdownModeProperty));
    }

    public async Task SetShutdownModeAsync(int shutdownMode)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(ShutdownModeProperty, shutdownMode.ToString());
    }

    public async Task<bool> GetEnableVisualStylesAsync()
    {
        await _threadingService.SwitchToUIThread();

        return bool.Parse(_myappDocument.GetProperty(EnableVisualStylesProperty));
    }

    public async Task SetEnableVisualStylesAsync(bool enableVisualStyles)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(EnableVisualStylesProperty, enableVisualStyles.ToString());
    }

    public async Task<int> GetAuthenticationModeAsync()
    {
        await _threadingService.SwitchToUIThread();

        return int.Parse(_myappDocument.GetProperty(AuthenticationModeProperty));
    }

    public async Task SetAuthenticationModeAsync(int authenticationMode)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(AuthenticationModeProperty, authenticationMode.ToString());
    }

    public async Task<bool> GetSaveMySettingsOnExitAsync()
    {
        await _threadingService.SwitchToUIThread();

        return bool.Parse(_myappDocument.GetProperty(SaveMySettingsOnExitProperty));
    }

    public async Task SetSaveMySettingsOnExitAsync(bool saveMySettingsOnExit)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(SaveMySettingsOnExitProperty, saveMySettingsOnExit.ToString());
    }

    public async  Task<int> GetHighDpiModeAsync()
    {
        await _threadingService.SwitchToUIThread();

        return int.Parse(_myappDocument.GetProperty(HighDpiModeProperty));
    }

    public async Task SetHighDpiModeAsync(int highDpiMode)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(HighDpiModeProperty, highDpiMode.ToString());
    }

    public async Task<string> GetSplashScreenAsync()
    {
        await _threadingService.SwitchToUIThread();

        return _myappDocument.GetProperty(SplashScreenProperty);
    }

    public async Task SetSplashScreenAsync(string splashScreen)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(SplashScreenProperty, splashScreen);
    }

    public async Task<int> GetMinimumSplashScreenDisplayTimeAsync()
    {
        await _threadingService.SwitchToUIThread();

        return int.Parse(_myappDocument.GetProperty(MinimumSplashScreenDisplayTimeProperty);
    }

    public async Task SetMinimumSplashScreenDisplayTimeAsync(int minimumSplashScreenDisplayTime)
    {
        await _threadingService.SwitchToUIThread();

        _myappDocument.SetProperty(MinimumSplashScreenDisplayTimeProperty, minimumSplashScreenDisplayTime.ToString());
    }
}
