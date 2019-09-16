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
                    if (_builder == null)
                        _builder = new StringBuilder();

                    // Need to factor in that when we eventually write to the logger
                    // it's going to append a new line to the string we write, so we 
                    // only append the new line just before we write another string.
                    if (_builder.Length != 0)
                    {
                        _builder.AppendLine();
                    }

                    _builder.Append(' ', 4 * _indentLevel);
                    _builder.AppendFormat(format);
                }
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
