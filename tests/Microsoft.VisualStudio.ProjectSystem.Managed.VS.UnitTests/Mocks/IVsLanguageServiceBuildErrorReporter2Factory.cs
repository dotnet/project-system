// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsLanguageServiceBuildErrorReporter2Factory
    {
        public static IVsLanguageServiceBuildErrorReporter2 ImplementClearErrors(Func<int> action)
        {
            var mock = new Mock<IVsReportExternalErrors>();

            var reporter = mock.As<IVsLanguageServiceBuildErrorReporter2>();
            reporter.Setup(r => r.ClearErrors())
                    .Returns(action);

            return reporter.Object;
        }

        public static IVsLanguageServiceBuildErrorReporter2 ImplementReportError(Action<string, string, VSTASKPRIORITY, int, int, int, int, string> action)
        {
            var mock = new Mock<IVsReportExternalErrors>();

            var reporter = mock.As<IVsLanguageServiceBuildErrorReporter2>();
            reporter.Setup(r => r.ReportError2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VSTASKPRIORITY>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                    .Callback(action);

            return reporter.Object;
        }
    }
}
