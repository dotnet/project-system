// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class SetAttributeCommand : IReferenceCommand
    {
        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly ProjectSystemReferenceUpdate _referenceUpdate;
        private readonly AbstractReferenceHandler _referenceHandler;

        public SetAttributeCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _referenceUpdate = referenceUpdate;
        }

        public async Task ExecuteAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == _referenceUpdate.ReferenceInfo.ItemSpecification);

            if (item != null)
            {
                await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, PropertySerializer.SimpleTypes.ToString(true), null);
            }
        }

        public async Task UndoAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == _referenceUpdate.ReferenceInfo.ItemSpecification);

            if (item != null)
            {
                await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, PropertySerializer.SimpleTypes.ToString(false), null);
            }
        }

        public Task RedoAsync() => ExecuteAsync();
    }
}
