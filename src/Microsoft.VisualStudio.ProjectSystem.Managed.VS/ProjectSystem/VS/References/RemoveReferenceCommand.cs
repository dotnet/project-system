// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class RemoveReferenceCommand : IReferenceCommand
    {

        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly ProjectSystemReferenceUpdate _referenceUpdate;
        private readonly AbstractReferenceHandler _referenceHandler;
        private Dictionary<string, string> _projectPropertiesValues;

        public RemoveReferenceCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            _referenceUpdate = referenceUpdate;
        }

        public async Task Execute()
        {
            _projectPropertiesValues = await _referenceHandler.GetAttributesAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);

            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
        }

        public async Task Undo()
        {
            await _referenceHandler.AddReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
            await _referenceHandler.SetAttributes(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo,
                _projectPropertiesValues);
        }

        public async Task Redo()
        {
            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
        }
    }
}
