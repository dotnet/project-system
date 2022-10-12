// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree;

/// <summary>
/// Wrapper around the VS Service that provides ImageMonikers
/// </summary>
[Export(typeof(IUIImageService))]
internal class UIImageService : IUIImageService
{
    private readonly IVsUIService<SVsImageService, IVsImageService2> _vsImageService;

    [ImportingConstructor]
    public UIImageService(IVsUIService<SVsImageService, IVsImageService2> vsImageService)
    {
        _vsImageService = vsImageService;
    }

    public ImageMoniker GetImageMonikerForFile(string filename)
    {
        return _vsImageService.Value.GetImageMonikerForFile(filename);
    }
}
