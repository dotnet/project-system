// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    [Export(typeof(VisualBasicNamespaceImportsList))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicNamespaceImportsList : UnconfiguredProjectHostBridge<IProjectVersionedValue<IProjectSubscriptionUpdate>, IProjectVersionedValue<ImmutableList<string>>, IProjectVersionedValue<ImmutableList<string>>>, IEnumerable<string>
    {
        private static readonly ImmutableHashSet<string> s_namespaceImportRule = Empty.OrdinalIgnoreCaseStringSet
            .Add(NamespaceImport.SchemaName);

        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;

        /// <summary>
        /// For unit testing purposes, to avoid having to mock all of CPS
        /// </summary>
        internal bool SkipInitialization { get; set; }

        [ImportingConstructor]
        public VisualBasicNamespaceImportsList(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        [Import(typeof(VisualBasicVSImports))]
        internal Lazy<VisualBasicVSImports>? VSImports { get; set; }

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

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.VisualBasic)]
        internal Task OnProjectFactoryCompletedAsync()
        {
            TryInitialize();

            return Task.CompletedTask;
        }

        protected override Task ApplyAsync(IProjectVersionedValue<ImmutableList<string>> value)
        {
            ImmutableList<string> current = AppliedValue?.Value ?? ImmutableList<string>.Empty;
            ImmutableList<string> input = value.Value;
            
            IEnumerable<string> removed = current.Except(input);
            IEnumerable<string> added = input.Except(current);

            AppliedValue = value;

            VisualBasicVSImports? imports = VSImports?.Value;

            if (imports != null)
            {
                foreach (string import in removed)
                {
                    imports.OnImportRemoved(import);
                }

                foreach (string import in added)
                {
                    imports.OnImportAdded(import);
                }
            }

            return Task.CompletedTask;
        }

        protected override Task<IProjectVersionedValue<ImmutableList<string>>> PreprocessAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> input, IProjectVersionedValue<ImmutableList<string>>? previousOutput)
        {
            IProjectChangeDescription projectChange = input.Value.ProjectChanges[NamespaceImport.SchemaName];

            return Task.FromResult<IProjectVersionedValue<ImmutableList<string>>>(
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
