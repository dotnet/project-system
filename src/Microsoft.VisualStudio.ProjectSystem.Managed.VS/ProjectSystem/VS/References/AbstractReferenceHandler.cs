// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            string itemSpecification)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Assumes.Present(configuredProject.Services);

            return RemoveReferenceAsync(configuredProject.Services, itemSpecification);
        }

        protected abstract Task RemoveReferenceAsync(ConfiguredProjectServices services,
            string itemSpecification);

        internal Task AddReferenceAsync(ConfiguredProject configuredProject,
            string itemSpecification)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Assumes.Present(configuredProject.Services);

            return AddReferenceAsync(configuredProject.Services, itemSpecification);
        }

        protected abstract Task AddReferenceAsync(ConfiguredProjectServices services,
            string itemSpecification);

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

            return value is not null && PropertySerializer.SimpleTypes.ToValue<bool>(value);
        }

        private async Task<IProjectItem?> GetProjectItemsAsync(ConfiguredProject selectedConfiguredProject,
            string itemSpecification)
        {
            var projectItems = await GetUnresolvedReferencesAsync(selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == itemSpecification);
            return item;
        }

        internal IProjectSystemUpdateReferenceOperation CreateSetAttributeCommand(ConfiguredProject selectedConfiguredProject,
            ProjectSystemReferenceUpdate referenceUpdate)
        {
            return new SetAttributeCommand(this, selectedConfiguredProject, referenceUpdate.ReferenceInfo.ItemSpecification);
        }

        internal IProjectSystemUpdateReferenceOperation CreateUnsetAttributeCommand(ConfiguredProject selectedConfiguredProject,
            ProjectSystemReferenceUpdate referenceUpdate)
        {
            return new UnsetAttributeCommand(this, selectedConfiguredProject, referenceUpdate.ReferenceInfo.ItemSpecification);
        }

        internal IProjectSystemUpdateReferenceOperation? CreateRemoveReferenceCommand(ConfiguredProject selectedConfiguredProject,
            ProjectSystemReferenceUpdate referenceUpdate)
        {
            return new RemoveReferenceCommand(this, selectedConfiguredProject, referenceUpdate);
        }

        public async Task<Dictionary<string, string>> GetAttributesAsync(ConfiguredProject selectedConfiguredProject, string itemSpecification)
        {
            Dictionary<string, string> propertyValues = new ();

            IProjectItem? items = await GetProjectItemsAsync(selectedConfiguredProject, itemSpecification);

            if (items is null)
            {
                return propertyValues;
            }

            var propertyNames = await items.Metadata.GetPropertyNamesAsync();

            foreach (var property in propertyNames)
            {
                var value = await items.Metadata.GetEvaluatedPropertyValueAsync(property);
                propertyValues.Add(property, value);
            }

            return propertyValues;
        }

        public async Task SetAttributesAsync(ConfiguredProject selectedConfiguredProject, string itemSpecification, Dictionary<string, string> projectPropertiesValues)
        {
            IProjectItem? items = await GetProjectItemsAsync(selectedConfiguredProject, itemSpecification);

            if (items is not null)
            {
                foreach ((string propertyName, string propertyValue) in projectPropertiesValues)
                {
                    await items.Metadata.SetPropertyValueAsync(propertyName, propertyValue, null);
                }
            }
        }
    }
}
