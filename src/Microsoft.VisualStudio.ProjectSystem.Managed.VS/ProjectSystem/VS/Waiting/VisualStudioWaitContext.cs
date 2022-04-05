// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    internal sealed class VisualStudioWaitContext : IWaitContext
    {
        private const int DelayToShowDialogSecs = 2;

        private readonly string _title;
        private readonly CancellationTokenSource? _cancellationTokenSource;
        private readonly IVsThreadedWaitDialog3 _dialog;

        private string _message;
        private string? _progressText;
        private int _currentStep;
        private int _totalSteps;

        public VisualStudioWaitContext(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, int totalSteps = 0)
        {
            _title = title;
            _message = message;
            _totalSteps = totalSteps;

            if (allowCancel)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _dialog = CreateDialog(waitDialogFactory);
        }

        private IVsThreadedWaitDialog3 CreateDialog(IVsThreadedWaitDialogFactory dialogFactory)
        {
            Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out IVsThreadedWaitDialog2 dialog2));

            Assumes.NotNull(dialog2);

            var dialog3 = (IVsThreadedWaitDialog3)dialog2;
            var callback = new Callback(_cancellationTokenSource);

            dialog3.StartWaitDialogWithCallback(
                szWaitCaption: _title,
                szWaitMessage: _message,
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                fIsCancelable: _cancellationTokenSource is not null,
                iDelayToShowDialog: DelayToShowDialogSecs,
                fShowProgress: _totalSteps != 0,
                iTotalSteps: _totalSteps,
                iCurrentStep: 0,
                pCallback: callback);

            return dialog3;
        }

        private class Callback : IVsThreadedWaitDialogCallback
        {
            private readonly CancellationTokenSource? _cancellationTokenSource;

            public Callback(CancellationTokenSource? cancellationTokenSource)
            {
                _cancellationTokenSource = cancellationTokenSource;
            }

            public void OnCanceled()
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        public void Update(string? message = null, int? currentStep = null, int? totalSteps = null, string? progressText = null)
        {
            bool hasChange = false;

            if (message is not null && !Equals(_message, message))
            {
                _message = message;
                hasChange = true;
            }

            if (totalSteps is not null && totalSteps != _totalSteps)
            {
                _totalSteps = totalSteps.Value;
                hasChange = true;
            }
            
            if (currentStep is not null && currentStep != _currentStep)
            {
                Requires.Argument(currentStep <= _totalSteps, nameof(currentStep), $"Must be less than or equal to the total number of steps.");

                _currentStep = currentStep.Value;
                hasChange = true;
            }

            if (!Equals(progressText, _progressText))
            {
                _progressText = progressText;
                hasChange = true;
            }

            if (hasChange)
            {
                _dialog.UpdateProgress(
                    _message,
                    szProgressText: _progressText,
                    szStatusBarText: null,
                    iCurrentStep: _currentStep,
                    iTotalSteps: _totalSteps,
                    fDisableCancel: _cancellationTokenSource is null,
                    pfCanceled: out _);
            }
        }

        public void Dispose()
        {
            _dialog.EndWaitDialog(out _);
            _cancellationTokenSource?.Dispose();
        }
    }
}
