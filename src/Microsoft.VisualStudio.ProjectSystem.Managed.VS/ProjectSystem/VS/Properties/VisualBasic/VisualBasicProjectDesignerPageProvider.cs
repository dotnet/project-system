// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    /// <summary>
    ///     Provides project designer property pages.
    /// </summary>
    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo(ProjectCapability.VisualBasicAppDesigner)]
    internal class VisualBasicProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        private readonly IProjectCapabilitiesService _capabilities;

        [ImportingConstructor]
        internal VisualBasicProjectDesignerPageProvider(IProjectCapabilitiesService capabilities)
        {
            _capabilities = capabilities;
        }

        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            var builder = PooledArray<IPageMetadata>.GetInstance(capacity: 7);

            builder.Add(VisualBasicProjectDesignerPage.Application);
            builder.Add(VisualBasicProjectDesignerPage.Compile);

            if (_capabilities.Contains(ProjectCapability.Pack))
            {
                builder.Add(VisualBasicProjectDesignerPage.Package);
            }

            builder.Add(VisualBasicProjectDesignerPage.References);

            if (_capabilities.Contains(ProjectCapability.LaunchProfiles))
            {
                builder.Add(VisualBasicProjectDesignerPage.Debug);
            }

            builder.Add(VisualBasicProjectDesignerPage.Signing);
            builder.Add(VisualBasicProjectDesignerPage.CodeAnalysis);

            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(builder.ToImmutableAndFree());
        }
    }
}
