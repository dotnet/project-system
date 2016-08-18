// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    /// <summary>
    /// Interface definition for a profile which you can modify
    /// </summary>
    public interface IMutableLaunchProfile
    {
        string Name { get; set; }
        string CommandName{ get; set;}
        string ExecutablePath { get; set;}
        string CommandLineArgs{ get; set;}
        string WorkingDirectory{ get; set;}
        bool LaunchBrowser { get; set;}
        string LaunchUrl { get; set;}
        Dictionary<string, object> EnvironmentVariables { get; set; }
        Dictionary<string, object> OtherSettings { get; set; }
    }
}
