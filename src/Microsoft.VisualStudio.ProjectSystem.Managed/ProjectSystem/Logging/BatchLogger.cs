// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Begins a logging batch that batches up logging writes
    ///     and writes them all at once on <see cref="IDisposable.Dispose" />.
    /// </summary>
    internal sealed class BatchLogger : IDisposable
    {
        private readonly IManagedProjectDiagnosticOutputService _outputService;
        private StringBuilder? _builder;
        private int _indentLevel;

        public BatchLogger(IManagedProjectDiagnosticOutputService outputService)
        {
            Requires.NotNull(outputService, nameof(outputService));

            _outputService = outputService;
        }

        /// <summary>
        ///     Gets or sets the indent level.
        /// </summary>
        /// <value>
        ///     The indent level. The default is 0.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="value"/> is less than 0.
        /// </exception>
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
            get { return _outputService.IsEnabled; }
        }

        internal void WriteLine()
        {
            if (IsEnabled)
            {
                _builder ??= new StringBuilder();

                _builder.AppendLine();
            }
        }

        public void WriteLine(string text)
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
                _builder.Append(text);
            }
        }

        public void Dispose()
        {
            if (_builder is not null)
            {
                _outputService.WriteLine(_builder.ToString());
            }
        }
    }
}
