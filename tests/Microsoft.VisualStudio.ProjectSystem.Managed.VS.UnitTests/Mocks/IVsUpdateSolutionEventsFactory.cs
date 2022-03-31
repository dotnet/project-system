// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
