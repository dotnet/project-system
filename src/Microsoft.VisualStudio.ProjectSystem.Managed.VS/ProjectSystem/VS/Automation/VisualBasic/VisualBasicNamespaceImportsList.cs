// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    [Export(typeof(VisualBasicNamespaceImportsList))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicNamespaceImportsList : OnceInitializedOnceDisposed, IEnumerable<string>
    {
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;

        private readonly object _lock = new object();
        private readonly List<string> _list = new List<string>();
        private IDisposable? _namespaceImportSubscriptionLink;

        private static readonly ImmutableHashSet<string> s_namespaceImportRule = Empty.OrdinalIgnoreCaseStringSet
            .Add(NamespaceImport.SchemaName);

        [ImportingConstructor]
        public VisualBasicNamespaceImportsList(IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
        {
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field '_activeConfiguredProjectSubscriptionService' is uninitialized
        internal VisualBasicNamespaceImportsList()
#pragma warning restore CS8618 // Non-nullable field '_activeConfiguredProjectSubscriptionService' is uninitialized
        {
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        internal void SetList(IEnumerable<string> list)
        {
            _list.Clear();
            _list.AddRange(list);
        }

        internal int Count => _list.Count;

        /// <summary>
        /// Returns an enumerator for the list of imports.
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            lock (_lock)
            {
                return _list.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.VisualBasic)]
        internal Task OnProjectFactoryCompletedAsync()
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        internal void OnNamespaceImportChanged(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            IProjectChangeDescription projectChange = e.Value.ProjectChanges[NamespaceImport.SchemaName];

            if (projectChange.Difference.AnyChanges)
            {
                lock (_lock)
                {
                    IOrderedEnumerable<string> sortedItems = projectChange.After.Items.Keys.OrderBy(s => s, StringComparers.ItemNames);
                    int newListCount = sortedItems.Count();
                    int oldListCount = _list.Count;

                    int trackingIndex = 0;

                    while (trackingIndex < oldListCount && trackingIndex < newListCount)
                    {
                        string incomingItem = sortedItems.ElementAt(trackingIndex);
                        if (string.Equals(_list[trackingIndex], incomingItem, StringComparisons.ItemNames))
                        {
                            trackingIndex++;
                            continue;
                        }

                        _list[trackingIndex] = incomingItem;
                        trackingIndex++;
                    }

                    if (oldListCount == newListCount)
                    {
                        return;
                    }
                    else if (oldListCount < newListCount)
                    {
                        _list.AddRange(sortedItems.Skip(trackingIndex));
                    }
                    else
                    {
                        _list.RemoveRange(trackingIndex, oldListCount - trackingIndex);
                    }
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

            lock (_lock)
            {
                if (lIndex > _list.Count)
                {
                    throw new ArgumentException(string.Format("{0} - Index value is greater than the length of the namespace import list.", lIndex), nameof(lIndex));
                }

                return _list[lIndex - 1];
            }
        }

        internal bool IsPresent(int indexInt)
        {
            if (indexInt < 1)
            {
                throw new ArgumentException(string.Format("{0} - Index value is less than One.", indexInt), nameof(indexInt));
            }

            lock (_lock)
            {
                if (indexInt > _list.Count)
                {
                    throw new ArgumentException(string.Format("{0} - Index value is greater than the length of the namespace import list.", indexInt), nameof(indexInt));
                }

                return true;
            }
        }

        internal bool IsPresent(string bstrImport)
        {
            if (string.IsNullOrEmpty(bstrImport))
            {
                throw new ArgumentException("The string cannot be null or empty", nameof(bstrImport));
            }

            lock (_lock)
            {
                return _list.Any(l => string.Equals(bstrImport, l, StringComparisons.ItemNames));
            }
        }

        protected override void Initialize()
        {
            //set up a subscription to listen for namespace import changes
            _namespaceImportSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAction(
                target: OnNamespaceImportChanged,
                ruleNames: s_namespaceImportRule);
        }

        protected override void Dispose(bool disposing)
        {
            _namespaceImportSubscriptionLink?.Dispose();
        }
    }
}
