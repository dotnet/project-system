// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectLogger"/> that logs to the Output window.
    /// </summary>
    [Export(typeof(IProjectLogger))]
    internal class ProjectOutputWindowProjectLogger : IProjectLogger
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectSystemOptions _options;
        private readonly IProjectOutputWindowPaneProvider _outputWindowProvider;

        [ImportingConstructor]
        public ProjectOutputWindowProjectLogger(IProjectThreadingService threadingService, IProjectSystemOptions options, IProjectOutputWindowPaneProvider outputWindowProvider)
        {
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(options, nameof(options));
            Requires.NotNull(outputWindowProvider, nameof(outputWindowProvider));

            _threadingService = threadingService;
            _options = options;
            _outputWindowProvider = outputWindowProvider;
        }

        public bool IsEnabled
        {
            get { return _options.IsProjectOutputPaneEnabled; }
        }
        public void WriteLine(string text)
        {
            WriteLine(new FormatArray(text));
        }

        public void WriteLine(string format, object argument)
        {
            WriteLine(new FormatArray(format, argument));
        }

        public void WriteLine(string format, object argument1, object argument2)
        {
            WriteLine(new FormatArray(format, argument1, argument2));
        }

        public void WriteLine(string format, object argument1, object argument2, object argument3)
        {
            WriteLine(new FormatArray(format, argument1, argument2, argument3));
        }

        public void WriteLine(string format, params object[] arguments)
        {
            WriteLine(new FormatArray(format, arguments));
        }

        private void WriteLine(FormatArray formatArray)
        {
            if (IsEnabled)
            {
                // Extremely naive implementation of a Windows Pane logger - the assumption here is that text is rarely written,
                // so transitions to the UI thread are uncommon and are fire and forget. If we start writing to this a lot (such 
                // as via build), then we'll need to implement a better queueing mechanism.
                _threadingService.Fork(async () =>
                {

                    IVsOutputWindowPane pane = await _outputWindowProvider.GetOutputWindowPaneAsync()
                                                                          .ConfigureAwait(true);

                    pane.OutputStringNoPump(formatArray.Text + Environment.NewLine);

                }, options: ForkOptions.HideLocks | ForkOptions.StartOnMainThread);
            }
        }
    }
}
