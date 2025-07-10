﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem;

internal static partial class IProjectThreadingServiceFactory
{
    public static IProjectThreadingService Create(bool verifyOnUIThread = true)
    {
        return new ProjectThreadingService(verifyOnUIThread);
    }
}
