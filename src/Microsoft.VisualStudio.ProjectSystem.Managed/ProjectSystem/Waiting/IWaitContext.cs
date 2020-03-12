// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
