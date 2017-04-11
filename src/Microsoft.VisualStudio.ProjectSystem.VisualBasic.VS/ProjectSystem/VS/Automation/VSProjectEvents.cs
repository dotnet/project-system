// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(VSLangProj.VSProjectEvents))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    [Order(10)]
    internal class VSProjectEvents : VSLangProj.VSProjectEvents
    {
        private readonly VSLangProj.VSProject _vsProject;

        [ImportingConstructor]
        public VSProjectEvents(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            UnconfiguredProject project)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(project, nameof(project));

            _vsProject = vsProject;
            ImportsEventsImpl = new OrderPrecedenceImportCollection<ImportsEvents>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ImportsEvents> ImportsEventsImpl { get; set; }

        public ReferencesEvents ReferencesEvents => _vsProject.Events.ReferencesEvents;

        public BuildManagerEvents BuildManagerEvents => _vsProject.Events.BuildManagerEvents;

        public ImportsEvents ImportsEvents => ImportsEventsImpl.First().Value;
    }
}
