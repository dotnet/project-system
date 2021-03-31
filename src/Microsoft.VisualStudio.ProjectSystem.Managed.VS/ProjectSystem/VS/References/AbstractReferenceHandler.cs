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

        internal Task AddReferenceAsync(ConfiguredProject configuredProject,
            ProjectSystemReferenceInfo reference)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Assumes.Present(configuredProject.Services);

            return AddReferenceAsync(configuredProject.Services, reference);
        }

        protected abstract Task AddReferenceAsync(ConfiguredProjectServices services,
            ProjectSystemReferenceInfo referencesInfo);

        public Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProject selectedConfiguredProject)
        {
            Requires.NotNull(selectedConfiguredProject, nameof(selectedConfiguredProject));
            Assumes.Present(selectedConfiguredProject.Services);

            return GetUnresolvedReferencesAsync(selectedConfiguredProject.Services);
        }

        protected abstract Task<IEnumerable<IProjectItem>> GetUnresolvedReferencesAsync(ConfiguredProjectServices services);

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
            var propertyNames = await metadata.GetPropertyNamesAsync();
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

        internal IReferenceCommand CreateUpdateReferenceCommand(ConfiguredProject selectedConfiguredProject,
            ProjectSystemReferenceUpdate referenceUpdate)
        {
            if (referenceUpdate.Action == ProjectSystemUpdateAction.SetTreatAsUsed)
            {
                return new SetAttributeCommand(this, selectedConfiguredProject, referenceUpdate);
            }

            return new UnSetAttributeCommand(this, selectedConfiguredProject, referenceUpdate); ;
        }

        internal IReferenceCommand? CreateRemoveReferenceCommand(ConfiguredProject selectedConfiguredProject,
            ProjectSystemReferenceUpdate referenceUpdate)
        {
            return new RemoveReferenceCommand(this, selectedConfiguredProject, referenceUpdate);
        }

        public async Task<Dictionary<string, string>> GetAttributesAsync(ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceInfo referenceInfo)
        {
            Dictionary<string, string> propertyValues = new Dictionary<string, string>();

            var projectItems = await GetUnresolvedReferencesAsync(selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == referenceInfo.ItemSpecification);

            var propertyNames = await item.Metadata.GetPropertyNamesAsync();

            foreach (var property in propertyNames)
            {
                var value = await item.Metadata.GetEvaluatedPropertyValueAsync(property);
                propertyValues.Add(string.Copy(property), string.Copy(value));
            }

            return propertyValues;
        }

        public async Task SetAttributes(ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceInfo referenceUpdateReferenceInfo, Dictionary<string, string> projectPropertiesValues)
        {
            var projectItems = await GetUnresolvedReferencesAsync(selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == referenceUpdateReferenceInfo.ItemSpecification);

            if (item != null)
            {
                foreach (var property in projectPropertiesValues)
                {
                    await item.Metadata.SetPropertyValueAsync(property.Key, property.Value, null);
                }
            }
        }
    }
}
