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
            _threadingService = threadingService;
            _options = options;
            _outputWindowProvider = outputWindowProvider;
        }

        public bool IsEnabled
        {
            get { return _options.IsProjectOutputPaneEnabled; }
        }

        public void WriteLine(in StringFormat format)
        {
            if (IsEnabled)
            {
                string text = format.Text + Environment.NewLine;

                // Extremely naive implementation of a Windows Pane logger - the assumption here is that text is rarely written,
                // so transitions to the UI thread are uncommon and are fire and forget. If we start writing to this a lot (such
                // as via build), then we'll need to implement a better queueing mechanism.
                _threadingService.RunAndForget(async () =>
                {
                    IVsOutputWindowPane? pane = await _outputWindowProvider.GetOutputWindowPaneAsync();

                    pane?.OutputStringNoPump(text);

                }, options: ForkOptions.HideLocks | ForkOptions.StartOnMainThread,
                   configuredProject: null);    // Not tied to one particular project
            }
        }
    }
}
