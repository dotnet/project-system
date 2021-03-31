// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class SetAttributeCommand : IReferenceCommand
    {
        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly string _itemSpecification;
        private readonly AbstractReferenceHandler _referenceHandler;

        public SetAttributeCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, string itemSpecification)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _itemSpecification = itemSpecification;
        }

        public async Task ExecuteAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == _itemSpecification);

            if (item != null)
            {
                await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, PropertySerializer.SimpleTypes.ToString(true), null);
            }
        }

        public async Task UndoAsync()
        {
            var projectItems = await _referenceHandler.GetUnresolvedReferencesAsync(_selectedConfiguredProject);

            var item = projectItems
                .FirstOrDefault(c => c.EvaluatedInclude == _itemSpecification);

            if (item != null)
            {
                await item.Metadata.SetPropertyValueAsync(ProjectReference.TreatAsUsedProperty, PropertySerializer.SimpleTypes.ToString(false), null);
            }
        }

        public Task RedoAsync() => ExecuteAsync();
    }
}
