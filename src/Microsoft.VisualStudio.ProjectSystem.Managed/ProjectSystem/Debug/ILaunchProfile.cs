// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    public enum ProfileKind
    {
        Project,           // Run the project executable
        IISExpress,
        BuiltInCommand,
        CustomizedCommand,
        Executable,
        IIS,
        NoAction            // This is the profile used when there is no profiles. It is a dummy placeholder
    }

    /// <summary>
    /// Interface definition for a profile
    /// </summary>
    public interface ILaunchProfile
    {
        string Name { get; }
        ProfileKind Kind { get; }
        string CommandName{ get; }
        string ExecutablePath { get; }
        string CommandLineArgs{ get; }
        string WorkingDirectory{ get; }
        bool LaunchBrowser { get; }
        string LaunchUrl { get; }
        string ApplicationUrl { get; }
        ImmutableDictionary<string, string> EnvironmentVariables { get; }
        ImmutableDictionary<string, object> OtherSettings { get;}
        string SDKVersion { get; }
        bool IsDefaultIISExpressProfile { get; }
        bool IsWebServerCmdProfile { get; }
    }
}
