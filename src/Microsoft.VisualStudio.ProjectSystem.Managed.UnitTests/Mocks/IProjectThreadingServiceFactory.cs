// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectThreadingServiceFactory
    {
        public static IProjectThreadingService Create()
        {
            return new IProjectThreadingServiceMock();
        }
    }
}
