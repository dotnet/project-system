// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class RemoveReferenceCommand : IProjectSystemUpdateReferenceOperation
    {
        private readonly AbstractReferenceHandler _referenceHandler;
        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly string _itemSpecification;

        private Dictionary<string, string>? _projectPropertiesValues;

        public RemoveReferenceCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _itemSpecification = referenceUpdate.ReferenceInfo.ItemSpecification;
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            _projectPropertiesValues = await _referenceHandler.GetAttributesAsync(_selectedConfiguredProject, _itemSpecification);

            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _itemSpecification);
            return true;
        }

        public async Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            await _referenceHandler.AddReferenceAsync(_selectedConfiguredProject, _itemSpecification);

            if (_projectPropertiesValues is not null)
            {
                await _referenceHandler.SetAttributesAsync(_selectedConfiguredProject, _itemSpecification, _projectPropertiesValues);
            }
            
            return true;
        }
    }
}
