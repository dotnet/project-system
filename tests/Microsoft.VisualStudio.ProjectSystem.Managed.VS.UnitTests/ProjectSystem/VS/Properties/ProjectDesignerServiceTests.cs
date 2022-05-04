// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class ProjectDesignerServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SupportsProjectDesigner_ReturnsResultIsProjectDesignerSupported(bool supportsProjectDesigner)
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => supportsProjectDesigner);

            var designerService = CreateInstance(vsProjectDesignerPageService);

            var result = designerService.SupportsProjectDesigner;

            Assert.Equal(supportsProjectDesigner, result);
        }

        [Fact]
        public void ShowProjectDesignerAsync_WhenSupportsProjectDesignerFalse_ThrowsInvalidOperation()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => false);

            var designerService = CreateInstance(vsProjectDesignerPageService);

            Assert.ThrowsAsync<InvalidOperationException>(designerService.ShowProjectDesignerAsync);
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenGetGuidPropertyForProjectDesignerEditorReturnsHResult_Throws()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            await Assert.ThrowsAsync<COMException>(designerService.ShowProjectDesignerAsync);
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenProjectDesignerEditorReturnsHResult_Throws()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            await Assert.ThrowsAsync<COMException>(designerService.ShowProjectDesignerAsync);
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenOpenItemWithSpecificEditorReturnsHResult_Throws()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);

            var project = (IVsProject4)hierarchy;
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            await Assert.ThrowsAsync<COMException>(designerService.ShowProjectDesignerAsync);
        }

        [Fact]
        public Task ShowProjectDesignerAsync_WhenOpenedInExternalEditor_DoesNotAttemptToShowWindow()
        {   // OpenItemWithSpecific returns null frame when opened in external editor
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);

            var project = (IVsProject4)hierarchy;
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, null);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            return designerService.ShowProjectDesignerAsync();
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenWindowShowReturnsHResult_Throws()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);
            var project = (IVsProject4)hierarchy;

            var frame = IVsWindowFrameFactory.ImplementShow(() => VSConstants.E_FAIL);
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, frame);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            await Assert.ThrowsAsync<COMException>(designerService.ShowProjectDesignerAsync);
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenOpenedInInternalEditor_ShowsWindow()
        {
            var vsProjectDesignerPageService = IVsProjectDesignerPageServiceFactory.ImplementIsProjectDesignerSupported(() => true);

            var editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);
            var project = (IVsProject4)hierarchy;

            int callCount = 0;
            var frame = IVsWindowFrameFactory.ImplementShow(() => { callCount++; return 0; });
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, frame);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices, vsProjectDesignerPageService);

            await designerService.ShowProjectDesignerAsync();

            Assert.Equal(1, callCount);
        }

        private static ProjectDesignerService CreateInstance(IVsProjectDesignerPageService vsProjectDesignerPageService)
        {
            return CreateInstance(null, vsProjectDesignerPageService);
        }

        private static ProjectDesignerService CreateInstance(IUnconfiguredProjectVsServices? projectVsServices, IVsProjectDesignerPageService? vsProjectDesignerPageService)
        {
            projectVsServices ??= IUnconfiguredProjectVsServicesFactory.Create();
            vsProjectDesignerPageService ??= IVsProjectDesignerPageServiceFactory.Create();

            return new ProjectDesignerService(projectVsServices, vsProjectDesignerPageService);
        }
    }
}
