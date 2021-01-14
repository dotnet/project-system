// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal abstract class AbstractReferenceHandler
    {
        private readonly ProjectSystemReferenceType _referenceType;

        protected AbstractReferenceHandler(ProjectSystemReferenceType referenceType)
            => _referenceType = referenceType;

        internal Task RemoveReferenceAsync(ConfiguredProject configuredProject,
            ProjectSystemReferenceInfo reference)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Assumes.Present(configuredProject.Services);

            return RemoveReferenceAsync(configuredProject.Services, reference);
        }

        protected abstract Task RemoveReferenceAsync(ConfiguredProjectServices services,
            ProjectSystemReferenceInfo referencesInfo);

        private Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProject selectedConfiguredProject)
        {
            Requires.NotNull(selectedConfiguredProject, nameof(selectedConfiguredProject));
            Assumes.Present(selectedConfiguredProject.Services);

            return GetUnresolvedReferencesAsync(selectedConfiguredProject.Services);
        }

        protected abstract Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services);

        internal async Task<bool> CanRemoveReferenceAsync(ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            var references = await GetReferencesAsync(selectedConfiguredProject, cancellationToken);

            ProjectSystemReferenceInfo referenceInfo = references.FirstOrDefault(c => c.ItemSpecification == referenceUpdate.ReferenceInfo.ItemSpecification);

            return !(referenceInfo is null);
        }

        internal async Task<List<ProjectSystemReferenceInfo>> GetReferencesAsync(ConfiguredProject selectedConfiguredProject, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var references = new List<ProjectSystemReferenceInfo>();

            var projectItems = await GetUnresolvedReferencesAsync(selectedConfiguredProject);

            foreach (var item in projectItems)
            {
                bool treatAsUsed = await GetAttributeTreatAsUsedAsync(item.Metadata);
                string itemSpecification = item.EvaluatedInclude;

                references.Add(new ProjectSystemReferenceInfo(_referenceType, itemSpecification, treatAsUsed));
            }

            return references;
        }

        private static async Task<bool> GetAttributeTreatAsUsedAsync(IProjectProperties metadata)
        {
            string? value = await metadata.GetEvaluatedPropertyValueAsync(ProjectReference.TreatAsUsedProperty);

            return value != null && PropertySerializer.SimpleTypes.ToValue<bool>(value);
        }

        internal async Task<bool> UpdateReferenceAsync(ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
        {
            bool wasUpdated = false;

            cancellationToken.ThrowIfCancellationRequested();

            var projectItems = await GetUnresolvedReferencesAsync(selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == referenceUpdate.ReferenceInfo.ItemSpecification);

            if (item != null)
            {
                string newValue = PropertySerializer.SimpleTypes.ToString(referenceUpdate.Action == ProjectSystemUpdateAction.SetTreatAsUsed);

                await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, newValue, null);

                wasUpdated = true;
            }

            return wasUpdated;
        }
    }
}
