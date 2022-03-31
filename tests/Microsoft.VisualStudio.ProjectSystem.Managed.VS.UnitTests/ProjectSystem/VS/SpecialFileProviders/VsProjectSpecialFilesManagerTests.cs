// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

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
