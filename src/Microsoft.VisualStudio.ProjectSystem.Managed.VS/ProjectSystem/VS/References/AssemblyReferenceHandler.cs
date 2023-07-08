// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class AssemblyReferenceHandler : AbstractReferenceHandler
    {
        internal AssemblyReferenceHandler()
            : base(ProjectSystemReferenceType.Assembly)
        { }

        protected override Task RemoveReferenceAsync(ConfiguredProjectServices services,
            string itemSpecification)
        {
            Assumes.Present(services.AssemblyReferences);

            AssemblyName? assemblyName = null;
            string? assemblyPath = null;

            if (Path.IsPathRooted(itemSpecification))
            {
                assemblyPath = itemSpecification;
            }
            else
            {
                assemblyName = new AssemblyName(itemSpecification);
            }

            return services.AssemblyReferences.RemoveAsync(assemblyName, assemblyPath);
        }

        protected override Task AddReferenceAsync(ConfiguredProjectServices services, string itemSpecification)
        {
            Assumes.Present(services.AssemblyReferences);

            AssemblyName? assemblyName = null;
            string? assemblyPath = null;

            if (Path.IsPathRooted(itemSpecification))
            {
                assemblyPath = itemSpecification;
            }
            else
            {
                assemblyName = new AssemblyName(itemSpecification);
            }

            // todo: get path from the Remove Command
            return services.AssemblyReferences.AddAsync(assemblyName, assemblyPath);
        }

        protected override async Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services)
        {
            Assumes.Present(services.AssemblyReferences);

            return (await services.AssemblyReferences.GetUnresolvedReferencesAsync()).Cast<IProjectItem>();
        }
    }
}
