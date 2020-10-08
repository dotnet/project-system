// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Responsible for activating and deactivating <see cref="IImplicitlyActiveService"/> instances.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(LoadCapabilities)]
    internal partial class ConfiguredProjectImplicitActivationTracking : AbstractMultiLifetimeComponent<ConfiguredProjectImplicitActivationTracking.ConfiguredProjectImplicitActivationTrackingInstance>, IProjectDynamicLoadComponent
    {
        // NOTE: Ideally this component would be marked with 'AlwaysApplicable' so that we always load
        // IImplicitlyActiveService instances in all project types regardless of exported capabilities,
        // but doing so would cause the .NET Project System's assemblies to be loaded in lots of 
        // situations even when not needed. Instead, we explicitly hardcode the set of capabilities of 
        // all our IImplicitlyActiveService services.
        private const string LoadCapabilities = ProjectCapability.DotNetLanguageService + " | " +
                                                ProjectCapability.PackageReferences;

        private readonly IProjectThreadingService _threadingService;
        private readonly ConfiguredProject _project;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;

        [ImportingConstructor]
        public ConfiguredProjectImplicitActivationTracking(
            IProjectThreadingService threadingService,
            ConfiguredProject project,
            IActiveConfigurationGroupService activeConfigurationGroupService)
            : base(threadingService.JoinableTaskContext)
        {
            _threadingService = threadingService;
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;

            ImplicitlyActiveServices = new OrderPrecedenceImportCollection<IImplicitlyActiveService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IImplicitlyActiveService> ImplicitlyActiveServices { get; }

        protected override ConfiguredProjectImplicitActivationTrackingInstance CreateInstance()
        {
            return new ConfiguredProjectImplicitActivationTrackingInstance(
                _threadingService,
                _project,
                _activeConfigurationGroupService,
                ImplicitlyActiveServices);
        }
    }
}
