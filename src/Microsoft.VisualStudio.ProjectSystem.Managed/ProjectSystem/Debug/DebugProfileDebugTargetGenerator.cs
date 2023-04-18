// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Provides the set of debug profiles to populate the debugger dropdown.  The Property associated
    /// with this is the ActiveDebugProfile which contains the currently selected profile, and the DebugProfiles which
    /// is the name of the enumerator provider
    /// </summary>
    [ExportDynamicEnumValuesProvider("DebugProfileProvider")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Export(typeof(IDynamicDebugTargetsGenerator))]
    [ExportMetadata("Name", "DebugProfileProvider")]
    internal class DebugProfileDebugTargetGenerator : ChainedProjectValueDataSourceBase<IReadOnlyList<IEnumValue>>, IDynamicEnumValuesProvider, IDynamicDebugTargetsGenerator
    {
        private readonly IVersionedLaunchSettingsProvider _launchSettingProvider;
        private readonly IProjectThreadingService _projectThreadingService;

        [ImportingConstructor]
        public DebugProfileDebugTargetGenerator(
            UnconfiguredProject project,
            IVersionedLaunchSettingsProvider launchSettingProvider,
            IProjectThreadingService threadingService)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            _launchSettingProvider = launchSettingProvider;
            _projectThreadingService = threadingService;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(
                new DebugProfileEnumValuesGenerator(_launchSettingProvider, _projectThreadingService));
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<IReadOnlyList<IEnumValue>>> targetBlock)
        {
            var transformBlock = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<ILaunchSettings>, IProjectVersionedValue<IReadOnlyList<IEnumValue>>>(
                update => update.Derive(Transform));

            // IVersionedLaunchSettingsProvider implements "SourceBlock" in both ILaunchSettingsProvider and IProjectValueDataSource<T>. Cast to the one we need.
            IProjectValueDataSource<ILaunchSettings> launchSettingsSource = _launchSettingProvider;

            return new DisposableBag
            {
                launchSettingsSource.SourceBlock.LinkTo(transformBlock, linkOptions: DataflowOption.PropagateCompletion),

                transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion),

                JoinUpstreamDataSources(_launchSettingProvider)
            };

            static IReadOnlyList<IEnumValue> Transform(ILaunchSettings launchSettings)
            {
                return DebugProfileEnumValuesGenerator.GetEnumeratorEnumValues(launchSettings);
            }
        }
    }
}

