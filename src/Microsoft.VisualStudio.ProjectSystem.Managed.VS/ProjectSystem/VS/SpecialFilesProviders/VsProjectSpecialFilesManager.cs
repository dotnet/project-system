// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.SpecialFilesProviders
{
    /// <summary>
    ///     Provides an implementation of <see cref="ISpecialFilesManager"/> that wraps <see cref="IVsProjectSpecialFiles"/>
    /// </summary>
    [Export(typeof(ISpecialFilesManager))]
    internal class VsProjectSpecialFilesManager : ISpecialFilesManager
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public VsProjectSpecialFilesManager(IUnconfiguredProjectVsServices projectVsServices)
        {
            _projectVsServices = projectVsServices;
        }

        public async Task<string?> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags)
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            var files = (IVsProjectSpecialFiles)_projectVsServices.VsHierarchy;
            HResult result = files.GetFile((int)fileId, (uint)flags, out _, out string fileName);
            if (result.IsOK)
                return fileName;

            if (result.IsNotImplemented)
                return null;    // Not handled

            throw result.Exception!;
        }
    }
}
