// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class RemoveReferenceCommand : IReferenceCommand
    {

        private readonly ConfiguredProject _selectedConfiguredProject;
        private readonly ProjectSystemReferenceUpdate _referenceUpdate;
        private readonly AbstractReferenceHandler _referenceHandler;

        public RemoveReferenceCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate)
        {
            _referenceHandler = abstractReferenceHandler;
            _selectedConfiguredProject = selectedConfiguredProject;
            // todo: create copy instead of reference
            _referenceUpdate = referenceUpdate;
        }

        public async Task Execute()
        {
            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
        }

        public async Task Undo()
        {
            await _referenceHandler.AddReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
        }

        public async Task Redo()
        {
            await _referenceHandler.RemoveReferenceAsync(_selectedConfiguredProject, _referenceUpdate.ReferenceInfo);
        }
    }
}
