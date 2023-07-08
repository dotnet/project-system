// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class PackageReferenceHandler : AbstractReferenceHandler
    {
        internal PackageReferenceHandler()
            : base(ProjectSystemReferenceType.Package)
        { }

        protected override Task RemoveReferenceAsync(ConfiguredProjectServices services,
            string itemSpecification)
        {
            Assumes.Present(services.PackageReferences);

            return services.PackageReferences.RemoveAsync(itemSpecification);
        }

        protected override Task AddReferenceAsync(ConfiguredProjectServices services, string itemSpecification)
        {
            Assumes.Present(services.PackageReferences);

            // todo: Get the Version from the Remove Command
            return services.PackageReferences.AddAsync(itemSpecification, "");
        }

        protected override async Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services)
        {
            Assumes.Present(services.PackageReferences);

            return (await services.PackageReferences.GetUnresolvedReferencesAsync()).Cast<IProjectItem>();
        }
    }
}
