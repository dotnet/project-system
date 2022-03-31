// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.CSharp
{
    /// <summary>
    ///     Provides project designer property pages.
    /// </summary>
    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo(ProjectCapability.CSharpAppDesigner)]
    internal class CSharpProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        private readonly IProjectCapabilitiesService _capabilities;

        [ImportingConstructor]
        internal CSharpProjectDesignerPageProvider(IProjectCapabilitiesService capabilities)
        {
            _capabilities = capabilities;
        }

        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            var builder = PooledArray<IPageMetadata>.GetInstance(capacity: 7);

            builder.Add(CSharpProjectDesignerPage.Application);
            builder.Add(CSharpProjectDesignerPage.Build);
            builder.Add(CSharpProjectDesignerPage.BuildEvents);

            if (_capabilities.Contains(ProjectCapability.Pack))
            {
                builder.Add(CSharpProjectDesignerPage.Package);
            }

            if (_capabilities.Contains(ProjectCapability.LaunchProfiles))
            {
                builder.Add(CSharpProjectDesignerPage.Debug);
            }

            builder.Add(CSharpProjectDesignerPage.Signing);
            builder.Add(CSharpProjectDesignerPage.CodeAnalysis);

            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(builder.ToImmutableAndFree());
        }
    }
}
