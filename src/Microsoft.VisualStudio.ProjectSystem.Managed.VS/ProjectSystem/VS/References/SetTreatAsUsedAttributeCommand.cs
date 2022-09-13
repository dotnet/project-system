// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal abstract class SetTreatAsUsedAttributeCommand : IProjectSystemUpdateReferenceOperation
    {
        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly string _itemSpecification;
        private readonly AbstractReferenceHandler _referenceHandler;
        protected string SetTreatAsUsed = PropertySerializer.SimpleTypes.ToString(true);
        protected string UnsetTreatAsUsed = PropertySerializer.SimpleTypes.ToString(false);

        public SetTreatAsUsedAttributeCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, string itemSpecification)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _itemSpecification = itemSpecification;
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            IProjectItem item = await GetProjectItemAsync();

            if (item is null)
            {
                return false;
            }

            await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, SetTreatAsUsed);

            return true;
        }

        public async Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            IProjectItem item = await GetProjectItemAsync();

            if (item is null)
            {
                return false;
            }

            await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, UnsetTreatAsUsed);

            return true;
        }

        private async Task<IProjectItem> GetProjectItemAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => string.CompareOrdinal(c.EvaluatedInclude, _itemSpecification) == 0);
            return item;
        }
    }
}
