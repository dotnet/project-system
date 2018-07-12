// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ITargetedProjectContext : IDisposable
    {
        string DisplayName { get; set; }
        string ProjectFilePath { get; set; }
        ITargetFramework TargetFramework { get; }
        string TargetPath { get; }
        bool LastDesignTimeBuildSucceeded { get; set; }
    }
}
