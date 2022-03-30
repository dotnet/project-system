// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.FSharp
{
    /// <summary>
    ///     Provides project designer property pages.
    /// </summary>
    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo(ProjectCapability.FSharpAppDesigner)]
    internal class FSharpProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        private readonly IProjectCapabilitiesService _capabilities;

        [ImportingConstructor]
        internal FSharpProjectDesignerPageProvider(IProjectCapabilitiesService capabilities)
        {
            _capabilities = capabilities;
        }

        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            var builder = PooledArray<IPageMetadata>.GetInstance(capacity: 7);

            builder.Add(FSharpProjectDesignerPage.Application);
            builder.Add(FSharpProjectDesignerPage.Build);
            builder.Add(FSharpProjectDesignerPage.BuildEvents);

            if (_capabilities.Contains(ProjectCapability.LaunchProfiles))
            {
                builder.Add(FSharpProjectDesignerPage.Debug);
            }

            if (_capabilities.Contains(ProjectCapability.Pack))
            {
                builder.Add(FSharpProjectDesignerPage.Package);
            }

            builder.Add(FSharpProjectDesignerPage.ReferencePaths);
            builder.Add(FSharpProjectDesignerPage.Signing);

            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(builder.ToImmutableAndFree());
        }
    }
}
