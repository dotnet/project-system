// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsLanguageServiceBuildErrorReporter2Factory
    {
        public static IVsLanguageServiceBuildErrorReporter ImplementClearErrors(Func<int> action)
        {
            var mock = new Mock<IVsReportExternalErrors>();

            var reporter = mock.As<IVsLanguageServiceBuildErrorReporter>();
            reporter.Setup(r => r.ClearErrors())
                    .Returns(action);

            return reporter.Object;
        }

        public static IVsLanguageServiceBuildErrorReporter ImplementReportError(Func<string, string, VSTASKPRIORITY, int, int, string, int> action)
        {
            var mock = new Mock<IVsReportExternalErrors>();

            var reporter = mock.As<IVsLanguageServiceBuildErrorReporter>();
            reporter.Setup(r => r.ReportError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VSTASKPRIORITY>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                    .Returns(action);

            return reporter.Object;
        }
    }
}
