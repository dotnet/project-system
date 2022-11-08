// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

    /// <summary>
    /// Contains constants representing the property names used in the ApplicationPropertyPage folder.
    /// </summary>
    internal static class PropertyNames
    {
        // Project properties
        internal const string UseWPF = "UseWPF";
        internal const string UseWindowsForms = "UseWindowsForms";

        // MSBuild properties
        internal const string ApplicationFrameworkMSBuild = "MyType";
        internal const string OutputTypeMSBuild = "OutputType";
        internal const string StartupObjectMSBuild = "StartupObject";

        // Application.myapp properties
        internal const string ApplicationFramework = "UseApplicationFramework";
        internal const string EnableVisualStyles = "EnableVisualStyles";
        internal const string SingleInstance = "SingleInstance";
        internal const string SaveMySettingsOnExit = "SaveMySettingsOnExit";
        internal const string HighDpiMode = "HighDpiMode";
        internal const string AuthenticationMode = "VBAuthenticationMode";
        internal const string ShutdownMode = "ShutdownMode";
        internal const string SplashScreen = "SplashScreen";
        internal const string MinimumSplashScreenDisplayTime = "MinimumSplashScreenDisplayTime";
    }
