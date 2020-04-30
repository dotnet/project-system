// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.Common;
using NuGet.ProjectModel;

namespace Microsoft.VisualStudio.NuGet.Models
{
    /// <summary>
    /// Models a diagnostic found in the assets file. Immutable.
    /// </summary>
    internal readonly struct AssetsFileLogMessage
    {
        public AssetsFileLogMessage(IAssetsLogMessage logMessage)
        {
            Code = logMessage.Code;
            Level = logMessage.Level;
            WarningLevel = logMessage.WarningLevel;
            Message = logMessage.Message;
            LibraryName = logMessage.LibraryId;
        }

        public NuGetLogCode Code { get; }
        public LogLevel Level { get; }
        public WarningLevel WarningLevel { get; }
        public string Message { get; }
        public string LibraryName { get; }

        public bool Equals(IAssetsLogMessage other)
        {
            return other.Code == Code
                && other.Level == Level
                && other.WarningLevel == WarningLevel
                && other.Message == Message
                && other.LibraryId == LibraryName;
        }

        public override string ToString() => $"{Level} {Code} ({LibraryName}) {Message}";
    }
}
