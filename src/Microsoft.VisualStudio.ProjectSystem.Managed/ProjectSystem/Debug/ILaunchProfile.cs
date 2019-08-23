// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a profile
    /// </summary>
    public interface ILaunchProfile
    {
        string Name { get; }
        string CommandName { get; }
        string ExecutablePath { get; }
        string CommandLineArgs { get; }
        string WorkingDirectory { get; }
        bool LaunchBrowser { get; }
        string LaunchUrl { get; }
        ImmutableDictionary<string, string> EnvironmentVariables { get; }
        ImmutableDictionary<string, object> OtherSettings { get; }
    }
}
