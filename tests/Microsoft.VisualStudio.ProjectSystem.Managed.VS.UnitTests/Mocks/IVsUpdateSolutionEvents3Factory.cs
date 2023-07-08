// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsUpdateSolutionEvents3Factory
    {
        public static IVsUpdateSolutionEvents3 Create(
            Action? onUpdateSolutionBegin = null,
            Action? onUpdateSolutionCancel = null,
            Action? onUpdateSolutionDone = null)
        {
            var solutionEventsListener = new Mock<IVsUpdateSolutionEvents3>();

            var anyInt = It.IsAny<int>();
            solutionEventsListener.As<IVsUpdateSolutionEvents>().Setup(b => b.UpdateSolution_Begin(ref anyInt))
                .Callback(() => onUpdateSolutionBegin?.Invoke())
                .Returns(VSConstants.S_OK);

            solutionEventsListener.As<IVsUpdateSolutionEvents>().Setup(b => b.UpdateSolution_Cancel())
                    .Callback(() => onUpdateSolutionCancel?.Invoke())
                    .Returns(VSConstants.S_OK);

            solutionEventsListener.As<IVsUpdateSolutionEvents>().Setup(b => b.UpdateSolution_Done(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                    .Callback(() => onUpdateSolutionDone?.Invoke())
                    .Returns(VSConstants.S_OK);

            return solutionEventsListener.Object;
        }
    }
}
