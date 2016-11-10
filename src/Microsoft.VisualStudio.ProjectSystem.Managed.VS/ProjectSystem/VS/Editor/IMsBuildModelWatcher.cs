// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface IMsBuildModelWatcher
    {
        Task InitializeAsync(string tempFile);
        void ProjectXmlHandler(object sender, ProjectXmlChangedEventArgs args);
        void Dispose();
    }
}