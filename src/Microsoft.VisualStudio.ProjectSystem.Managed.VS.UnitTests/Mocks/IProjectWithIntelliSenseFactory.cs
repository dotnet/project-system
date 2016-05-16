// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IProjectWithIntellisenseFactory
    {
        public static IProjectWithIntellisense ImplementGetExternalErrorReporter(IVsLanguageServiceBuildErrorReporter reportExternalErrorsReporter)
        {
            return ImplementGetExternalErrorReporter((IVsReportExternalErrors)reportExternalErrorsReporter);
        }

        public static IProjectWithIntellisense ImplementGetExternalErrorReporter(IVsReportExternalErrors reportExternalErrorsReporter)
        {
            var project = new Mock<IVsIntellisenseProject>();
            project.Setup(h => h.GetExternalErrorReporter(out reportExternalErrorsReporter))
                .Returns(0);

            var mock = new Mock<IProjectWithIntellisense>();
            mock.SetupGet(h => h.IntellisenseProject)
                .Returns(project.Object);

            return mock.Object;
        }
    }
}
