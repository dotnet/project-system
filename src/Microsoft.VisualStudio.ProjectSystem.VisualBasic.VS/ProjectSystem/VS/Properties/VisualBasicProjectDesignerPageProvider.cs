// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
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
            var builder = ImmutableArray.CreateBuilder<IPageMetadata>();
            builder.Add(VisualBasicProjectDesignerPage.Application);

            if (_capabilities.Contains(ProjectCapability.Pack))
            {
                builder.Add(VisualBasicProjectDesignerPage.Package);
            }

            builder.Add(VisualBasicProjectDesignerPage.References);
            builder.Add(VisualBasicProjectDesignerPage.Debug);

            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(builder.ToImmutable());
        }
    }
}
