// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal class PackageRestoreUpToDateTelemetryEvent
    {
        internal PackageRestoreUpToDateTelemetryEvent(string packageRestoreOperationName, string fullPath, bool isRestoreUpToDate)
        {
            PackageRestoreOperationName = packageRestoreOperationName;
            FullPath = fullPath;
            IsRestoreUpToDate = isRestoreUpToDate;
        }

        internal string PackageRestoreOperationName { get; }

        internal string FullPath { get; }

        internal bool IsRestoreUpToDate { get; }
    }
}
