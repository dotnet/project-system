// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    internal sealed partial class DependenciesGraphProvider
    {
        /// <summary>
        /// Maps between the representation of icons used by <see cref="IVsImageService2"/> and the rest of the dependencies node,
        /// caching the mapping data for performance.
        /// </summary>
        private sealed class GraphIconCache
        {
            private ImmutableHashSet<ImageMoniker> _registeredIcons = ImmutableHashSet<ImageMoniker>.Empty;

            private ImmutableDictionary<(int id, Guid guid), string> _iconNameCache = ImmutableDictionary<(int id, Guid guid), string>.Empty;

            private readonly IVsImageService2 _imageService;

            public static async Task<GraphIconCache> CreateAsync(IAsyncServiceProvider serviceProvider)
            {
#pragma warning disable RS0030 // Do not used banned APIs
                var imageService = (IVsImageService2)await serviceProvider.GetServiceAsync(typeof(SVsImageService));
#pragma warning restore RS0030 // Do not used banned APIs

                return new GraphIconCache(imageService);
            }

            private GraphIconCache(IVsImageService2 imageService) => _imageService = imageService;

            public string GetName(ImageMoniker icon)
            {
                return ImmutableInterlocked.GetOrAdd(ref _iconNameCache, (id: icon.Id, guid: icon.Guid), i => $"{i.guid:D};{i.id}");
            }

            public void Register(ImageMoniker icon)
            {
                if (ImmutableInterlocked.Update(ref _registeredIcons, (knownIcons, arg) => knownIcons.Add(arg), icon))
                {
                    _imageService.TryAssociateNameWithMoniker(GetName(icon), icon);
                }
            }
        }
    }
}
