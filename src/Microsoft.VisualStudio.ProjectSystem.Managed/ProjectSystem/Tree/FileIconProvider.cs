// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree
{
    [Export(typeof(IFileIconProvider))]
    internal sealed class FileIconProvider : IFileIconProvider
    {
        private readonly IUIImageService _imageService;

        private ImmutableDictionary<string, ImageMoniker> _imageMonikerByExtensions = ImmutableDictionary.Create<string, ImageMoniker>(StringComparers.Paths);

        [ImportingConstructor]
        public FileIconProvider(IUIImageService imageService)
        {
            _imageService = imageService;
        }

        public ImageMoniker GetFileExtensionImageMoniker(string path)
        {
            Requires.NotNull(path, nameof(path));

            string extension = Path.GetExtension(path);

            return ImmutableInterlocked.GetOrAdd(ref _imageMonikerByExtensions, extension, GetImageMoniker, path);

            ImageMoniker GetImageMoniker(string _, string p)
            {
                ImageMoniker imageMoniker = _imageService.GetImageMonikerForFile(p);

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
