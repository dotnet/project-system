// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    internal partial class ProjectLoggerExtensions
    {
        private class ProjectLoggerBatch : IProjectLoggerBatch
        {
            private readonly IProjectLogger _logger;
            private StringBuilder? _builder;
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

            public void WriteLine(in StringFormat format)
            {
                if (IsEnabled)
                {
                    StringBuilder builder = GetOrCreateBuilder();
                    WriteNewLineIfNeeded(builder);
                    WriteIndent(builder, _indentLevel);
                    Write(builder, format);
                }
            }

            private StringBuilder GetOrCreateBuilder()
            {
                if (_builder == null)
                    _builder = new StringBuilder();

                return _builder;
            }

            private static void WriteNewLineIfNeeded(StringBuilder builder)
            {
                // Need to factor in that when we eventually write to the logger
                // it's going to append a new line to the string we write, so we 
                // only append the new line just before we write another string.
                if (builder.Length != 0)
                {
                    builder.AppendLine();
                }
            }

            private static void WriteIndent(StringBuilder builder, int indentLevel)
            {
                for (int i = 0; i < indentLevel; i++)
                {
                    builder.Append("    ");
                }
            }

            private static void Write(StringBuilder builder, in StringFormat format)
            {
                builder.AppendFormat(format);
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
