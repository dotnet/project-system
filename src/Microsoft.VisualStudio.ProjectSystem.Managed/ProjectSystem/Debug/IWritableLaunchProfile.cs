// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a writable launch profile
    /// </summary>
    public interface IWritableLaunchProfile
    {
        string Name { get; set; }
        string CommandName { get; set; }
        string ExecutablePath { get; set; }
        string CommandLineArgs { get; set; }
        string WorkingDirectory { get; set; }
        bool LaunchBrowser { get; set; }
        string LaunchUrl { get; set; }
        Dictionary<string, string> EnvironmentVariables { get; }
        Dictionary<string, object> OtherSettings { get; }

        // Convert back to the immutable form
        ILaunchProfile ToLaunchProfile();
    }
}
