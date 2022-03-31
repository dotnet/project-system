// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Implementation of <see cref="IQueryDataSourceVersionProvider"/> for launch
    /// settings. Receives data from the <see cref="LaunchSettingsQueryVersionProvider"/>
    /// and passes it on to subscribed observers.
    /// </summary>
    /// <remarks>
    /// We need to be careful that this type is only loaded by the Project Query API.
    /// If our code were to load it, then we would cause the load of the Project Query
    /// assemblies in situations where they weren't actually loaded. To avoid this, our
    /// code has no direct dependency on this type, only on the <see cref="ILaunchSettingsVersionPublisher"/>
    /// interface (which is defined in the project system). When this type *is* loaded it
    /// registers itself with the <see cref="LaunchSettingsQueryVersionProvider"/>.
    /// </remarks>
    [Export(typeof(IQueryDataSourceVersionProvider))]
    [ExportMetadata("Name", LaunchSettingsQueryVersionGroupName)]
    internal class LaunchSettingsQueryVersionProviderExport : OnceInitializedOnceDisposed, IQueryDataSourceVersionProvider, ILaunchSettingsVersionPublisher
    {
        public const string LaunchSettingsQueryVersionGroupName = "LaunchSettings";

        /// <remarks>
        /// Responsible for generating new version information.
        /// </remarks>
        private readonly LaunchSettingsQueryVersionProvider _provider;
        /// <remarks>
        /// Processes notifications to the <see cref="_observers"/>.
        /// </remarks>
        private readonly ITargetBlock<Action> _processingBlock;
        /// <summary>
        /// The set of change notification observers.
        /// </summary>
        private ImmutableHashSet<IObserver<QueryDataSourceChangeNotification>> _observers = ImmutableHashSet<IObserver<QueryDataSourceChangeNotification>>.Empty;
        /// <remarks>
        /// Used to keep track of whether or not a new subscriber has received its initial snapshot.
        /// </remarks>
        private long _processedNotificationCount;
        /// <remarks>
        /// The current set of versions.
        /// </remarks>
        private QueryDataVersions _versions = QueryDataVersions.Empty;
        /// <remarks>
        /// The most recent set of versions sent to subscribers.
        /// </remarks>
        private QueryDataVersions _latestPostedVersions = QueryDataVersions.Empty;

        [ImportingConstructor]
        public LaunchSettingsQueryVersionProviderExport(LaunchSettingsQueryVersionProvider provider)
        {
            _provider = provider;
#pragma warning disable RS0030 // Do not used banned APIs
            // DataflowBlockFactory.CreateActionBlock(...) would be preferable, but it takes an
            // UnconfiguredProject as a parameter. This type isn't associated with any
            // particular project so we can't use it.
            _processingBlock = DataflowBlockSlim.CreateActionBlock<Action>(callback => callback(), nameFormat: nameof(LaunchSettingsQueryVersionProviderExport));
#pragma warning restore RS0030 // Do not used banned APIs

            _provider.BindToVersionPublisher(this);
        }

        public void UpdateVersions(ImmutableDictionary<string, long> versions)
        {
            lock (SyncObject)
            {
                _versions = QueryDataVersions.Empty.AddRange(versions);

                PostVersionUpdate();
            }
        }

        public IDisposable SubscribeChangeNotifications(IObserver<QueryDataSourceChangeNotification> observer)
        {
            EnsureInitialized();

            lock (SyncObject)
            {
                if (_observers.Contains(observer))
                {
                    Requires.Fail($"This observer is already subscribed to change notifications from the {nameof(LaunchSettingsQueryVersionProviderExport)}.");
                }

                _observers = _observers.Add(observer);

                // We don't immediately send the current set of versions to the observer. Instead,
                // we post a message to send that data later. However, by that point the observer
                // may have already received an up-to-date set of versions. To avoid double-sending
                // the data we keep track of how many version updates we've already sent. If that
                // number hasn't changed when we process the posted message, we know the observer
                // has not received any version data yet.
                long processedNotificationCountAtTimeOfSubscription = _processedNotificationCount;

                _processingBlock.Post(() =>
                {
                    bool observerRequiresNotification = false;
                    lock (SyncObject)
                    {
                        observerRequiresNotification = _observers.Contains(observer) && processedNotificationCountAtTimeOfSubscription == _processedNotificationCount;
                    }

                    if (observerRequiresNotification
                        && _latestPostedVersions != QueryDataVersions.Empty)
                    {
                        observer.OnNext(new QueryDataSourceChangeNotification(updates: _latestPostedVersions));
                    }
                });
            }

            return new Unsubscriber(this, observer);
        }

        public bool TryGetVersion(string versionKey, out long version)
        {
            Requires.NotNullOrEmpty(versionKey, nameof(versionKey));
            lock (SyncObject)
            {
                return _versions.TryGetValue(versionKey, out version);
            }
        }

        protected override void Initialize()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _versions = QueryDataVersions.Empty;
                _processingBlock.Complete();
            }
        }

        private void PostVersionUpdate()
        {
            lock (SyncObject)
            {
                if (IsInitialized)
                {
                    _processingBlock.Post(() =>
                    {
                        ImmutableHashSet<IObserver<QueryDataSourceChangeNotification>> observers;
                        QueryDataVersions newVersions;
                        lock (SyncObject)
                        {
                            if (_latestPostedVersions == _versions)
                            {
                                return;
                            }

                            observers = _observers;
                            newVersions = _versions;
                            _processedNotificationCount++;
                        }

                        if (observers.Count == 0)
                        {
                            return;
                        }

                        List<string>? expiredSources = null;
                        foreach (string versionKey in _latestPostedVersions.Keys)
                        {
                            if (!newVersions.ContainsKey(versionKey))
                            {
                                expiredSources ??= new List<string>();

                                expiredSources.Add(versionKey);
                            }
                        }

                        _latestPostedVersions = newVersions;
                        QueryDataSourceChangeNotification notification = new(newVersions, expiredSources?.ToImmutableArray());
                        foreach (IObserver<QueryDataSourceChangeNotification> observer in observers)
                        {
                            observer.OnNext(notification);
                        }
                    });
                }
            }
        }

        private class Unsubscriber : IDisposable
        {
            private readonly LaunchSettingsQueryVersionProviderExport _provider;
            private readonly IObserver<QueryDataSourceChangeNotification> _observer;

            public Unsubscriber(
                LaunchSettingsQueryVersionProviderExport provider,
                IObserver<QueryDataSourceChangeNotification> observer)
            {
                _provider = provider;
                _observer = observer;
            }

            public void Dispose()
            {
                lock (_provider.SyncObject)
                {
                    _provider._observers = _provider._observers.Remove(_observer);
                }
            }
        }
    }
}
