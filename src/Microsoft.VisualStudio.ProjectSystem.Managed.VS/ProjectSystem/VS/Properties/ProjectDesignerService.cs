// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectDesignerService"/> based on Visual Studio services.
    /// </summary>
    [Export(typeof(IProjectDesignerService))]
    internal class ProjectDesignerService : IProjectDesignerService
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsProjectDesignerPageService _vsProjectDesignerPageService;

        [ImportingConstructor]
        public ProjectDesignerService(IUnconfiguredProjectVsServices projectVsServices, IVsProjectDesignerPageService vsProjectDesignerPageService)
        {
            _projectVsServices = projectVsServices;
            _vsProjectDesignerPageService = vsProjectDesignerPageService;
        }

        public bool SupportsProjectDesigner => _vsProjectDesignerPageService.IsProjectDesignerSupported;

        public Task ShowProjectDesignerAsync()
        {
            if (SupportsProjectDesigner)
            {
                return OpenProjectDesignerCoreAsync();
            }

            throw new InvalidOperationException("This project does not support the Project Designer (SupportsProjectDesigner is false).");
        }

        private async Task OpenProjectDesignerCoreAsync()
        {
            Guid projectDesignerGuid = _projectVsServices.VsHierarchy.GetGuidProperty(VsHierarchyPropID.ProjectDesignerEditor);

            IVsWindowFrame? frame = _projectVsServices.VsProject.OpenItemWithSpecific(HierarchyId.Root, projectDesignerGuid);

            if (frame is not null)
            {   // Opened within Visual Studio
                // Can only use Shell APIs on the UI thread
                await _projectVsServices.ThreadingService.SwitchToUIThread();

                Verify.HResult(frame.Show());
            }
        }
    }
}
