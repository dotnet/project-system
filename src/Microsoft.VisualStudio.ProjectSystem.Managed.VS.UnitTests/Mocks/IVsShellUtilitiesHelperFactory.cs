// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell.Interop;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal static class IVsShellUtilitiesHelperFactory
    {
        public static IVsShellUtilitiesHelper Create() => Mock.Of<IVsShellUtilitiesHelper>();

        public static IVsShellUtilitiesHelper ImplementOpenDocument(string expectedFilePath, Guid expectedEditorType, Guid expectedLogicalView, IVsWindowFrame retFrame)
        {
            var utilities = new Mock<IVsShellUtilitiesHelper>(MockBehavior.Strict);
            utilities.Setup(u => u.OpenDocumentWithSpecificEditorAsync(It.IsAny<IServiceProvider>(), expectedFilePath, expectedEditorType, expectedLogicalView))
                .Returns(Task.FromResult(retFrame));
            return utilities.Object;
        }

        public static IVsShellUtilitiesHelper ImplementGetRDTInfo(string expectedFilePath, IVsPersistDocData retDocData)
        {
            var utilities = new Mock<IVsShellUtilitiesHelper>(MockBehavior.Strict);
            utilities.Setup(u => u.GetRDTDocumentInfoAsync(It.IsAny<IServiceProvider>(), expectedFilePath))
                .Returns(Task.FromResult<(IVsHierarchy, uint, IVsPersistDocData, uint)>((null, 0, retDocData, 0)));
            return utilities.Object;
        }
    }
}
