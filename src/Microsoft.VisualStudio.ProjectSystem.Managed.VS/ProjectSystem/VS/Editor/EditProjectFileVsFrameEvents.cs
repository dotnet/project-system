// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal class EditProjectFileVsFrameEvents : OnceInitializedOnceDisposed, IVsWindowFrameEvents
    {
        private readonly string _tempFile;
        private readonly IFileSystem _fileSystem;
        private readonly IMsBuildModelWatcher _watcher;
        private readonly IVsWindowFrame _frame;
        private readonly IServiceProviderHelper _helper;
        private readonly EditProjectFileCommand _command;
        private uint _eventCookie;

        public EditProjectFileVsFrameEvents(string tempFile, 
            IFileSystem fileSystem, 
            IMsBuildModelWatcher watcher, 
            IVsWindowFrame frame, 
            IServiceProviderHelper helper,
            EditProjectFileCommand command) : 
            base(synchronousDisposal: true)
        {
            _tempFile = tempFile;
            _fileSystem = fileSystem;
            _watcher = watcher;
            _frame = frame;
            _helper = helper;
            _command = command;
        }

        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            var uiShellService = _helper.GlobalProvider.GetService<IVsUIShell7, SVsUIShell>();
            _eventCookie = uiShellService.AdviseWindowFrameEvents(this);
        }

        public void InitializeEvents()
        {
            EnsureInitialized(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIThreadHelper.VerifyOnUIThread();
                _watcher.Dispose();
                _fileSystem.RemoveDirectory(Path.GetDirectoryName(_tempFile), true);

                var uiShellService = _helper.GlobalProvider.GetService<IVsUIShell7, SVsUIShell>();
                uiShellService.UnadviseWindowFrameEvents(_eventCookie);
                _command.Deinit();
            }
        }

        public void OnFrameDestroyed(IVsWindowFrame frame)
        {
            if (_frame.Equals(frame))
            {
                Dispose();
            }
        }

        public void OnActiveFrameChanged(IVsWindowFrame oldFrame, IVsWindowFrame newFrame)
        {
        }

        public void OnFrameCreated(IVsWindowFrame frame)
        {
        }

        public void OnFrameIsOnScreenChanged(IVsWindowFrame frame, bool newIsOnScreen)
        {
        }

        public void OnFrameIsVisibleChanged(IVsWindowFrame frame, bool newIsVisible)
        {
        }
    }
}
