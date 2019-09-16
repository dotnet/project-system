// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.SpecialFilesProviders
{
    public class VsProjectSpecialFilesManagerTests
    {
        [Fact]
        public async Task GetFiles_WhenUnderlyingGetFilesReturnsOK_ReturnsFileName()
        {
            var specialFiles = IVsProjectSpecialFilesFactory.ImplementGetFile((int fileId, uint flags, out uint itemId, out string fileName) =>
            {
                itemId = 0;
                fileName = "FileName";
                return VSConstants.S_OK;
            });

            var manager = CreateInstance(specialFiles);

            var result = await manager.GetFileAsync(SpecialFiles.AppConfig, SpecialFileFlags.CheckoutIfExists);

            Assert.Equal("FileName", result);
        }

        [Fact]
        public async Task GetFiles_WhenUnderlyingGetFilesReturnsNotImpl_ReturnsNull()
        {
            var specialFiles = IVsProjectSpecialFilesFactory.ImplementGetFile((int fileId, uint flags, out uint itemId, out string fileName) =>
            {
                itemId = 0;
                fileName = "FileName";
                return VSConstants.E_NOTIMPL;
            });

            var manager = CreateInstance(specialFiles);

            var result = await manager.GetFileAsync(SpecialFiles.AppConfig, SpecialFileFlags.CheckoutIfExists);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFiles_WhenUnderlyingGetFilesReturnsHResult_Throws()
        {
            var specialFiles = IVsProjectSpecialFilesFactory.ImplementGetFile((int fileId, uint flags, out uint itemId, out string fileName) =>
            {
                itemId = 0;
                fileName = "FileName";
                return VSConstants.E_OUTOFMEMORY;
            });

            var manager = CreateInstance(specialFiles);

            await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            {
                return manager.GetFileAsync(SpecialFiles.AppConfig, SpecialFileFlags.CheckoutIfExists);
            });
        }

        private VsProjectSpecialFilesManager CreateInstance(IVsProjectSpecialFiles specialFiles)
        {
            IUnconfiguredProjectVsServices projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(hierarchyCreator: () => (IVsHierarchy)specialFiles);

            return new VsProjectSpecialFilesManager(projectVsServices);
        }
    }
}
