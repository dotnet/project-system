using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(VisualBasicNamespaceImportsList))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicNamespaceImportsList : OnceInitializedOnceDisposed
    {
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;

        private readonly object _lock = new object();
        private List<string> _list;
        private IDisposable _namespaceImportSubscriptionLink;

        private static readonly ImmutableHashSet<string> s_namespaceImportRule = Empty.OrdinalIgnoreCaseStringSet
            .Add(NamespaceImport.SchemaName);

        [ImportingConstructor]
        public VisualBasicNamespaceImportsList(IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
        {
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));

            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        internal VisualBasicNamespaceImportsList()
        {
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        /// <param name="list"></param>
        internal void SetList(List<string> list)
        {
            _list = list;
        }

        internal int Count => _list.Count;

        /// <summary>
        /// Returns an enumerator for the list of imports. If the list is changed while using the enumerator, the enumerator will throw an exception
        /// to let the user know that the list has changed.
        /// </summary>
        /// <returns></returns>
        internal IEnumerator<string> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

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
                    var sortedItems = projectChange.After.Items.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase);
                    int newListCount = sortedItems.Count();
                    int oldListCount = _list.Count;

                    int trackingIndex = 0;

                    while (trackingIndex < oldListCount && trackingIndex < newListCount)
                    {
                        string incomingItem = sortedItems.ElementAt(trackingIndex);
                        if (string.Compare(_list[trackingIndex], incomingItem, StringComparison.OrdinalIgnoreCase) == 0)
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
                return _list.Any(l => string.Compare(bstrImport, l, StringComparison.OrdinalIgnoreCase) == 0);
            }
        }

        protected override void Initialize()
        {
            _list = new List<string>();

            //set up a subscription to listen for namespace import changes
            var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(e => OnNamespaceImportChanged(e));
            _namespaceImportSubscriptionLink = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                target: target,
                ruleNames: s_namespaceImportRule,
                initialDataAsNew: true,
                suppressVersionOnlyUpdates: true);
        }

        protected override void Dispose(bool disposing)
        {
            _namespaceImportSubscriptionLink?.Dispose();
        }
    }
}
