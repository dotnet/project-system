// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    internal interface IWaitContext : IDisposable
    {
        CancellationToken CancellationToken { get; }
        bool AllowCancel { get; set; }
        string Message { get; set; }
    }
}
