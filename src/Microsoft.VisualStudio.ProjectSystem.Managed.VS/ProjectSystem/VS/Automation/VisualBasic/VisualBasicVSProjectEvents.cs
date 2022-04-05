// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    [Export(typeof(VSProjectEvents))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    [Order(Order.Default)]
    internal class VisualBasicVSProjectEvents : VSProjectEvents
    {
        private readonly VSLangProj.VSProject _vsProject;

        [ImportingConstructor]
        public VisualBasicVSProjectEvents(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            UnconfiguredProject project)
        {
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
