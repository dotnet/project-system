// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
                    _builder ??= new StringBuilder();

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
