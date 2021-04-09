// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class RemoveReferenceCommand : IReferenceCommand
    {

        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly string _itemSpecification;
        private readonly AbstractReferenceHandler _referenceHandler;
        private Dictionary<string, string>? _projectPropertiesValues;

        public RemoveReferenceCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _itemSpecification = referenceUpdate.ReferenceInfo.ItemSpecification;
        }

        public async Task ExecuteAsync()
        {
            _projectPropertiesValues = await _referenceHandler.GetAttributesAsync(_selectedConfiguredProject, _itemSpecification);

            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _itemSpecification);
        }

        public async Task UndoAsync()
        {
            await _referenceHandler.AddReferenceAsync(_selectedConfiguredProject, _itemSpecification);
            await _referenceHandler.SetAttributes(_selectedConfiguredProject, _itemSpecification,
                _projectPropertiesValues);
        }

        public async Task RedoAsync()
        {
            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _itemSpecification);
        }
    }
}
