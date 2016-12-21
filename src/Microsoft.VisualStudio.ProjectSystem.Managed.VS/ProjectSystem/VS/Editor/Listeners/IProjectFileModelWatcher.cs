// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface IProjectFileModelWatcher : IDisposable
    {
        void Initialize();
    }
}
