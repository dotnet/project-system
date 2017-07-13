// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed class BuildUpToDateCheckLogger
    {
        private readonly TextWriter _logger;
        private readonly LogLevel _requestedLogLevel;
        private readonly string _fileName;

        public BuildUpToDateCheckLogger(TextWriter logger, LogLevel requestedLogLevel, string projectPath)
        {
            _logger = logger;
            _requestedLogLevel = requestedLogLevel;
            _fileName = Path.GetFileNameWithoutExtension(projectPath);
        }

        private void Log(LogLevel level, string message, params object[] values)
        {
            if (level <= _requestedLogLevel)
            {
                _logger?.WriteLine($"FastUpToDate: {string.Format(message, values)} ({_fileName})");
            }
        }

        public void Info(string message, params object[] values) => Log(LogLevel.Info, message, values);
        public void Verbose(string message, params object[] values) => Log(LogLevel.Verbose, message, values);
    }
}
