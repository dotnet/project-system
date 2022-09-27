// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

    /// <summary>
    /// Contains constants representing the property names used in the ApplicationPropertyPage folder.
    /// </summary>
    internal class PropertyNameProvider
    {
        // Project properties
        internal const string UseWPFProperty = "UseWPF";
        internal const string UseWindowsFormsProperty = "UseWindowsForms";

        // MSBuild properties
        internal const string ApplicationFrameworkMSBuildProperty = "MyType";
        internal const string OutputTypeMSBuildProperty = "OutputType";
        internal const string StartupObjectMSBuildProperty = "StartupObject";

        // Application.myapp properties
        internal const string ApplicationFrameworkProperty = "UseApplicationFramework";
        internal const string EnableVisualStylesProperty = "EnableVisualStyles";
        internal const string SingleInstanceProperty = "SingleInstance";
        internal const string SaveMySettingsOnExitProperty = "SaveMySettingsOnExit";
        internal const string HighDpiModeProperty = "HighDpiMode";
        internal const string AuthenticationModeProperty = "VBAuthenticationMode";
        internal const string ShutdownModeProperty = "ShutdownMode";
        internal const string SplashScreenProperty = "SplashScreen";
        internal const string MinimumSplashScreenDisplayTimeProperty = "MinimumSplashScreenDisplayTime";
    }
