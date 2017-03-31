// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(VSLangProj.VSProjectEvents))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VSProjectEvents : VSLangProj.VSProjectEvents
    {
        private readonly ImportsEvents _importsEvents;
        private readonly VSLangProj.VSProject _vsProject;

        [ImportingConstructor]
        public VSProjectEvents(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            ImportsEvents importsEvents)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(importsEvents, nameof(importsEvents));

            _vsProject = vsProject;
            _importsEvents = importsEvents;
        }

        public ReferencesEvents ReferencesEvents => _vsProject.Events.ReferencesEvents;

        public BuildManagerEvents BuildManagerEvents => _vsProject.Events.BuildManagerEvents;

        public ImportsEvents ImportsEvents => _importsEvents;
    }
}
