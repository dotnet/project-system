// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

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
    internal class DebugProfileDebugTargetGenerator : ProjectValueDataSourceBase<IReadOnlyList<IEnumValue>>, IDynamicEnumValuesProvider, IDynamicDebugTargetsGenerator
    {
        private IReceivableSourceBlock<IProjectVersionedValue<IReadOnlyList<IEnumValue>>>? _publicBlock;

        // Represents the link to the launch profiles
        private IDisposable? _launchProfileProviderLink;

        // Represents the link to our source provider
        private IDisposable? _debugProviderLink;

        [ImportingConstructor]
        public DebugProfileDebugTargetGenerator(
            UnconfiguredProject project,
            IVersionedLaunchSettingsProvider launchSettingProvider,
            IProjectThreadingService threadingService)
            : base(project.Services)
        {
            LaunchSettingProvider = launchSettingProvider;
            ProjectThreadingService = threadingService;
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(DebugProfileDebugTargetGenerator));

        private int _dataSourceVersion;

        public override IComparable DataSourceVersion
        {
            get { return _dataSourceVersion; }
        }

        public override IReceivableSourceBlock<IProjectVersionedValue<IReadOnlyList<IEnumValue>>> SourceBlock
        {
            get
            {
                EnsureInitialized();
                return _publicBlock!;
            }
        }

        private IVersionedLaunchSettingsProvider LaunchSettingProvider { get; }
        private IProjectThreadingService ProjectThreadingService { get; }

        /// <summary>
        /// This provides access to the class which creates the list of debugger values..
        /// </summary>
        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
            => Task.FromResult<IDynamicEnumValuesGenerator>(
                new DebugProfileEnumValuesGenerator(LaunchSettingProvider, ProjectThreadingService));

        protected override void Initialize()
        {
            IPropagatorBlock<IProjectVersionedValue<ILaunchSettings>, IProjectVersionedValue<IReadOnlyList<IEnumValue>>> debugProfilesBlock = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<ILaunchSettings>, IProjectVersionedValue<IReadOnlyList<IEnumValue>>>(
                update =>
                {
                    // Compute the new enum values from the profile provider
                    var generatedResult = DebugProfileEnumValuesGenerator.GetEnumeratorEnumValues(update.Value).ToImmutableList();
                    _dataSourceVersion++;
                    ImmutableDictionary<NamedIdentity, IComparable> dataSources = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(DataSourceKey, DataSourceVersion);
                    return new ProjectVersionedValue<IReadOnlyList<IEnumValue>>(generatedResult, dataSources);
                });

            IBroadcastBlock<IProjectVersionedValue<IReadOnlyList<IEnumValue>>> broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<IReadOnlyList<IEnumValue>>>(nameFormat: "Debug Profiles Broadcast: {1}");

            // The interface has two definitions of SourceBlock: one from
            // ILaunchSettingsProvider, and one from IProjectValueDataSource<T> (via
            // IVersionedLaunchSettingsProvider). We need the cast to pick the proper one.
            _launchProfileProviderLink = ((IProjectValueDataSource<ILaunchSettings>)LaunchSettingProvider).SourceBlock.LinkTo(
                debugProfilesBlock,
                linkOptions: DataflowOption.PropagateCompletion);

            JoinUpstreamDataSources(LaunchSettingProvider);

            _debugProviderLink = debugProfilesBlock.LinkTo(broadcastBlock, DataflowOption.PropagateCompletion);

            _publicBlock = broadcastBlock.SafePublicize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_launchProfileProviderLink is not null)
                {
                    _launchProfileProviderLink.Dispose();
                    _launchProfileProviderLink = null;
                }

                if (_debugProviderLink is not null)
                {
                    _debugProviderLink.Dispose();
                    _debugProviderLink = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}

