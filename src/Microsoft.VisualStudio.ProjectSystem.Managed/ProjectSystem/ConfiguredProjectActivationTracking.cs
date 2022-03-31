// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Responsible for activating and deactivating <see cref="IActiveConfigurationComponent"/> instances in
    ///     response to changes in either project capabilities or the active project configuration.
    /// </summary>
    /// <remarks>
    ///     Similar to <see cref="ConfiguredProjectImplicitActivationTracking"/> and <see cref="IImplicitlyActiveConfigurationComponent"/>,
    ///     however those types apply to <em>implicitly</em> active configurations (of which there may be multiple).
    ///     By contrast, this type applies to the singular active configuration.
    /// </remarks>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(LoadCapabilities)]
    internal partial class ConfiguredProjectActivationTracking : AbstractMultiLifetimeComponent<ConfiguredProjectActivationTracking.ConfiguredProjectActivationTrackingInstance>, IProjectDynamicLoadComponent
    {
        // NOTE: Ideally this component would be marked with 'AlwaysApplicable' so that we always load
        // IActiveConfigurationComponent instances in all project types regardless of exported capabilities,
        // but doing so would cause the .NET Project System's assemblies to be loaded in lots of 
        // situations even when not needed. Instead, we explicitly hard code the set of capabilities of 
        // all our IActiveConfigurationComponent services.
        private const string LoadCapabilities = "(" + ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject + ")";

        private readonly IProjectThreadingService _threadingService;
        private readonly ConfiguredProject _project;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;

        [ImportingConstructor]
        public ConfiguredProjectActivationTracking(
            IProjectThreadingService threadingService,
            ConfiguredProject project,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider)
            : base(threadingService.JoinableTaskContext)
        {
            _threadingService = threadingService;
            _project = project;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;

            Components = new OrderPrecedenceImportCollection<IActiveConfigurationComponent>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IActiveConfigurationComponent> Components { get; }

        protected override ConfiguredProjectActivationTrackingInstance CreateInstance()
        {
            return new(
                _threadingService,
                _project,
                _activeConfiguredProjectProvider,
                Components);
        }
    }
}
