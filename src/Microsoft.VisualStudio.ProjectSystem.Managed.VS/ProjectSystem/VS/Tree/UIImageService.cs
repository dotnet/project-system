// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree;

/// <summary>
/// Wrapper around the VS Service that provides ImageMonikers
/// </summary>
[Export(typeof(IUIImageService))]
internal class UIImageService : IUIImageService
{
    private readonly IVsUIService<SVsImageService, IVsImageService2> _vsImageService;
    private readonly JoinableTaskContext _context;

    [ImportingConstructor]
    public UIImageService(IVsUIService<SVsImageService, IVsImageService2> vsImageService, JoinableTaskContext context)
    {
        _vsImageService = vsImageService;
        _context = context;
    }

    public ImageMoniker GetImageMonikerForFile(string filename)
    {
        _context.VerifyIsOnMainThread();

        return _vsImageService.Value.GetImageMonikerForFile(filename);
    }
}
