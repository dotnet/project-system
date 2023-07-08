// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class ProjectReferenceHandler : AbstractReferenceHandler
    {
        internal ProjectReferenceHandler()
            : base(ProjectSystemReferenceType.Project)
        { }

        protected override Task RemoveReferenceAsync(ConfiguredProjectServices services,
            string itemSpecification)
        {
            Assumes.Present(services.ProjectReferences);

            return services.ProjectReferences.RemoveAsync(itemSpecification);
        }

        protected override Task AddReferenceAsync(ConfiguredProjectServices services, string itemSpecification)
        {
            Assumes.Present(services.ProjectReferences);

            return services.ProjectReferences.AddAsync(itemSpecification);
        }

        protected override async Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services)
        {
            Assumes.Present(services.ProjectReferences);

            return (await services.ProjectReferences.GetUnresolvedReferencesAsync()).Cast<IProjectItem>();
        }
    }
}
