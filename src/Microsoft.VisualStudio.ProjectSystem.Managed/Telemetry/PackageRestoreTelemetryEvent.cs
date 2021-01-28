// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal class PackageRestoreTelemetryEvent
    {
        internal PackageRestoreTelemetryEvent(string packageRestoreOperationName, string fullPath)
        {
            PackageRestoreOperationName = packageRestoreOperationName;
            FullPath = fullPath;
        }

        internal string PackageRestoreOperationName { get; }

        internal string FullPath { get; }
    }
}
