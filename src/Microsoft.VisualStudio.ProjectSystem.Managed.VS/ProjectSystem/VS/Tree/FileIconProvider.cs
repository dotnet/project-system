// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree
{
    [Export(typeof(IFileIconProvider))]
    internal sealed class FileIconProvider : IFileIconProvider
    {
        private readonly IVsUIService<SVsImageService, IVsImageService2> _vsImageService;

        private ImmutableDictionary<string, ImageMoniker> _imageMonikerByExtensions = ImmutableDictionary.Create<string, ImageMoniker>(StringComparers.Paths);

        [ImportingConstructor]
        public FileIconProvider(IVsUIService<SVsImageService, IVsImageService2> vsImageService)
        {
            _vsImageService = vsImageService;
        }

        public ImageMoniker GetFileExtensionImageMoniker(string path)
        {
            Requires.NotNull(path, nameof(path));

            string extension = Path.GetExtension(path);

            return ImmutableInterlocked.GetOrAdd(ref _imageMonikerByExtensions, extension, GetImageMoniker, path);

            ImageMoniker GetImageMoniker(string _, string p)
            {
                ImageMoniker imageMoniker = _vsImageService.Value.GetImageMonikerForFile(p);

                if (imageMoniker.Id == -1)
                {
                    // No specific icon exists for this extension
                    imageMoniker = KnownMonikers.Document;
                }

                return imageMoniker;
            }
        }
    }
}
