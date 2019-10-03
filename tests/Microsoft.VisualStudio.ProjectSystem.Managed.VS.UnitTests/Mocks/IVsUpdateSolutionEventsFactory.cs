// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsUpdateSolutionEventsFactory
    {
        public static IVsUpdateSolutionEvents Create(
            Action? onUpdateSolutionBegin = null,
            Action? onUpdateSolutionCancel = null,
            Action? onUpdateSolutionDone = null)
        {
            var solutionEventsListener = new Mock<IVsUpdateSolutionEvents>();

            var anyInt = It.IsAny<int>();
            solutionEventsListener.Setup(b => b.UpdateSolution_Begin(ref anyInt))
                .Callback(() => onUpdateSolutionBegin?.Invoke())
                .Returns(VSConstants.S_OK);

            solutionEventsListener.Setup(b => b.UpdateSolution_Cancel())
                    .Callback(() => onUpdateSolutionCancel?.Invoke())
                    .Returns(VSConstants.S_OK);

            solutionEventsListener.Setup(b => b.UpdateSolution_Done(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback(() => onUpdateSolutionDone?.Invoke())
                    .Returns(VSConstants.S_OK);

            return solutionEventsListener.Object;
        }
    }
}
