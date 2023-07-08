// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(DotNetNamespaceImportsList))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal class DotNetNamespaceImportsList : UnconfiguredProjectHostBridge<IProjectVersionedValue<IProjectSubscriptionUpdate>, IProjectVersionedValue<ImmutableList<string>>, IProjectVersionedValue<ImmutableList<string>>>, IEnumerable<string>
    {
        private static readonly ImmutableHashSet<string> s_namespaceImportRule = Empty.OrdinalIgnoreCaseStringSet
            .Add(NamespaceImport.SchemaName);

        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;

        /// <summary>
        /// For unit testing purposes, to avoid having to mock all of CPS
        /// </summary>
        internal bool SkipInitialization { get; set; }

        [ImportingConstructor]
        public DotNetNamespaceImportsList(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        [Import(typeof(DotNetVSImports))]
        internal Lazy<DotNetVSImports> VSImports { get; set; } = null!;

        /// <summary>
        /// Get the global project collection version number, so we can make sure we are waiting for the latest build after a dependent project is updated.
        /// </summary>
        [Import(ContractNames.MSBuild.GlobalProjectCollectionGlobalProperties, typeof(IProjectGlobalPropertiesProvider))]
        private IProjectGlobalPropertiesProvider GlobalProjectCollectionWatcher { get; set; } = null!;

        private void TryInitialize()
        {
            if (!SkipInitialization)
            {
                Initialize();
            }
        }

        internal int Count
        {
            get
            {
                TryInitialize();

                Assumes.NotNull(AppliedValue);

                return AppliedValue.Value.Count;
            }
        }

        /// <summary>
        /// Returns an enumerator for the list of imports.
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            TryInitialize();

            Assumes.NotNull(AppliedValue);

            return AppliedValue.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override async Task ApplyAsync(IProjectVersionedValue<ImmutableList<string>> value)
        {
            await JoinableFactory.SwitchToMainThreadAsync();

            IProjectVersionedValue<ImmutableList<string>>? previous = AppliedValue;

            AppliedValue = value;

            // To avoid callers seeing an inconsistent state where there are no Imports,
            // we use BlockInitializeOnFirstAppliedValue to block on the first value
            // being applied.
            //
            // Due to that, and to avoid a deadlock when event handlers call back into us
            // while we're still initializing, we avoid firing the events the first time 
            // a value is applied.
            if (previous is not null)
            {
                DotNetVSImports imports = VSImports.Value;
                ImmutableList<string> currentValue = value.Value;
                ImmutableList<string> previousValue = previous.Value;

                foreach (string import in previousValue.Except(currentValue))
                {
                    imports.OnImportRemoved(import);
                }

                foreach (string import in currentValue.Except(previousValue))
                {
                    imports.OnImportAdded(import);
                }
            }
        }

        protected override Task<IProjectVersionedValue<ImmutableList<string>>?> PreprocessAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> input, IProjectVersionedValue<ImmutableList<string>>? previousOutput)
        {
            IProjectChangeDescription projectChange = input.Value.ProjectChanges[NamespaceImport.SchemaName];

            return Task.FromResult<IProjectVersionedValue<ImmutableList<string>>?>(
                new ProjectVersionedValue<ImmutableList<string>>(
                    projectChange.After.Items.Keys.ToImmutableList(),
                    input.DataSourceVersions));
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> targetBlock)
        {
            JoinUpstreamDataSources(_activeConfiguredProjectSubscriptionService.ProjectRuleSource);

            //set up a subscription to listen for namespace import changes
            return _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(targetBlock,
                linkOptions: DataflowOption.PropagateCompletion,
                ruleNames: s_namespaceImportRule);
        }

        protected override Task InitializeInnerCoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Receive latest snapshot from project subscription service.
        /// </summary>
        /// <returns>returns a task, which the caller could await for</returns>
        internal async Task<IProjectVersionedValue<ImmutableList<string>>> ReceiveLatestSnapshotAsync()
        {
            await InitializeAsync();
            while (true)
            {
                try
                {
                    var minimumRequiredDataSourceVersions = ActiveConfiguredProjectProvider.ActiveConfiguredProject?.CreateVersionRequirement(allowMissingData: false).ToBuilder();

                    Assumes.NotNull(minimumRequiredDataSourceVersions);

                    IComparable? latestProjectCollectionVersion = GlobalProjectCollectionWatcher.DataSourceVersion;
                    minimumRequiredDataSourceVersions.Add(
                        ProjectDataSources.GlobalProjectCollectionGlobalProperties,
                        new ProjectVersionRequirement(latestProjectCollectionVersion!, allowMissingData: true));

                    return await ApplyAsync(minimumRequiredDataSourceVersions.ToImmutable(), default);
                }
                catch (ActiveProjectConfigurationChangedException)
                {
                    // The active project config has changed since we started.  Recalculate the data source versions
                    // we need and start waiting over again.
                }
            }
        }

        // lIndex is One-based index
        internal string Item(int lIndex)
        {
            if (lIndex < 1)
            {
                throw new ArgumentException(string.Format("{0} - Index value is less than One.", lIndex), nameof(lIndex));
            }

            TryInitialize();

            Assumes.NotNull(AppliedValue);

            ImmutableList<string> list = AppliedValue.Value;

            if (lIndex > list.Count)
            {
                throw new ArgumentException(string.Format("{0} - Index value is greater than the length of the namespace import list.", lIndex), nameof(lIndex));
            }

            return list[lIndex - 1];
        }

        internal bool IsPresent(int indexInt)
        {
            if (indexInt < 1)
            {
                throw new ArgumentException(string.Format("{0} - Index value is less than One.", indexInt), nameof(indexInt));
            }

            TryInitialize();

            Assumes.NotNull(AppliedValue);

            if (indexInt > AppliedValue.Value.Count)
            {
                throw new ArgumentException(string.Format("{0} - Index value is greater than the length of the namespace import list.", indexInt), nameof(indexInt));
            }

            return true;
        }

        internal bool IsPresent(string bstrImport)
        {
            if (string.IsNullOrEmpty(bstrImport))
            {
                throw new ArgumentException("The string cannot be null or empty", nameof(bstrImport));
            }

            TryInitialize();

            Assumes.NotNull(AppliedValue);

            return AppliedValue.Value.Any(l => string.Equals(bstrImport, l, StringComparisons.ItemNames));
        }
    }
}
