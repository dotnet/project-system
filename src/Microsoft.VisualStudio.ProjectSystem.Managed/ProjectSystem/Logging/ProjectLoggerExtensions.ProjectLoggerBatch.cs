// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    partial class ProjectLoggerExtensions
    {
        private class ProjectLoggerBatch : IProjectLoggerBatch
        {
            private readonly IProjectLogger _logger;
            private StringBuilder _builder;
            private int _indentLevel;

            internal ProjectLoggerBatch(IProjectLogger logger)
            {
                Assumes.NotNull(logger);

                _logger = logger;
            }

            public int IndentLevel
            {
                get { return _indentLevel; }
                set
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);

                    _indentLevel = value;
                }
            }

            public bool IsEnabled
            {
                get { return _logger.IsEnabled; }
            }

            public void WriteLine(StringFormat format)
            {
                if (IsEnabled)
                {
                    Init();
                    WriteNewLineIfNeeded();
                    WriteIndent();
                    Write(format);
                }
            }

            private void Init()
            {
                if (_builder == null)
                    _builder = new StringBuilder();
            }

            private void WriteNewLineIfNeeded()
            {
                // Need to factor in that when we eventually write to the logger
                // it's going to append a new line to the string we write, so we 
                // only append the new line just before we write another string.
                if (_builder.Length != 0)
                {
                    _builder.AppendLine();
                }
            }

            private void WriteIndent()
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    _builder.Append("    ");
                }
            }

            private void Write(StringFormat format)
            {
                _builder.AppendFormat(format);
            }

            public void Dispose()
            {
                if (_builder != null)
                {
                    _logger.WriteLine(_builder.ToString());
                }
            }
        }
    }
}
