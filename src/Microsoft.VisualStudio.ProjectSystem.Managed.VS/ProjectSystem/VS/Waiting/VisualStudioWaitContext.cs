// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    internal class VisualStudioWaitContext : IWaitContext
    {
        private const int DelayToShowDialogSecs = 2;

        private readonly string _title;
        private string _message;
        private bool _allowCancel;
        private readonly CancellationTokenSource? _cancellationTokenSource;
        private readonly IVsThreadedWaitDialog3 _dialog;

        public VisualStudioWaitContext(IVsThreadedWaitDialogFactory waitDialogFactory,
                                       string title,
                                       string message,
                                       bool allowCancel)
        {
            _title = title;
            _message = message;
            _allowCancel = allowCancel;
            if (_allowCancel)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _dialog = CreateDialog(waitDialogFactory);
        }

        private IVsThreadedWaitDialog3 CreateDialog(IVsThreadedWaitDialogFactory dialogFactory)
        {
            Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out IVsThreadedWaitDialog2 dialog2));
            if (dialog2 == null)
                throw new ArgumentNullException(nameof(dialog2));

            var dialog3 = (IVsThreadedWaitDialog3)dialog2;
            var callback = new Callback(this);

            dialog3.StartWaitDialogWithCallback(
                szWaitCaption: _title,
                szWaitMessage: _message,
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                fIsCancelable: _allowCancel,
                iDelayToShowDialog: DelayToShowDialogSecs,
                fShowProgress: false,
                iTotalSteps: 0,
                iCurrentStep: 0,
                pCallback: callback);

            return dialog3;
        }

        private class Callback : IVsThreadedWaitDialogCallback
        {
            private readonly VisualStudioWaitContext _waitContext;

            public Callback(VisualStudioWaitContext waitContext)
            {
                _waitContext = waitContext;
            }

            public void OnCanceled()
            {
                _waitContext.OnCanceled();
            }
        }

        public CancellationToken CancellationToken => _cancellationTokenSource is null
                                                    ? CancellationToken.None
                                                    : _cancellationTokenSource.Token;

        public bool AllowCancel
        {
            get => _allowCancel;
            set
            {
                _allowCancel = value;
                UpdateDialog();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                UpdateDialog();
            }
        }

        private void UpdateDialog()
        {
            _dialog.UpdateProgress(
                _message,
                szProgressText: null,
                szStatusBarText: null,
                iCurrentStep: 0,
                iTotalSteps: 0,
                fDisableCancel: !_allowCancel,
                pfCanceled: out _);
        }

        public void Dispose()
        {
            _dialog.EndWaitDialog(out _);
            _cancellationTokenSource?.Dispose();
        }

        private void OnCanceled()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
