// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    /// Language service host object for an unconfigured project that wraps the underlying host object.
    /// </summary>
    internal sealed class UnconfiguredProjectHostObject : AbstractHostObject, IUnconfiguredProjectHostObject
    {
        private readonly Dictionary<uint, IVsHierarchyEvents> _hierEventSinks;
        private readonly HashSet<uint> _pendingItemIds;

        public UnconfiguredProjectHostObject(UnconfiguredProject unconfiguredProject)
            : base (innerHierarchy: (IVsHierarchy)unconfiguredProject.Services.HostObject, innerVsProject: (IVsProject)unconfiguredProject.Services.HostObject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _hierEventSinks = new Dictionary<uint, IVsHierarchyEvents>();
            _pendingItemIds = new HashSet<uint>();
        }

        public IConfiguredProjectHostObject ActiveIntellisenseProjectHostObject { get; set; }
        public override string ActiveIntellisenseProjectDisplayName => ActiveIntellisenseProjectHostObject?.ProjectDisplayName;
        public bool DisposingConfiguredProjectHostObjects { get; set; }

        public void PushPendingIntellisenseProjectHostObjectUpdates()
        {
            Requires.Range(!DisposingConfiguredProjectHostObjects, nameof(DisposingConfiguredProjectHostObjects));

            foreach (var itemId in _pendingItemIds)
            {
                OnPropertyChanged(itemId, (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy);
            }

            _pendingItemIds.Clear();
        }

        #region IVsHierarchy overrides

        public override int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            var hr = base.AdviseHierarchyEvents(pEventSink, out pdwCookie);
            if (hr == VSConstants.S_OK)
            {
                _hierEventSinks.Add(pdwCookie, pEventSink);
            }

            return hr;
        }

        public override int GetProperty(uint itemid, int propid, out object pvar)
        {
            if (ActiveIntellisenseProjectHostObject != null)
            {
                switch (propid)
                {
                    case (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext:
                        pvar = ActiveIntellisenseProjectDisplayName;
                        return VSConstants.S_OK;

                    case (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy:
                        pvar = ActiveIntellisenseProjectHostObject;
                        return VSConstants.S_OK;
                }
            }

            return base.GetProperty(itemid, propid, out pvar);
        }

        public override int SetProperty(uint itemid, int propid, object var)
        {
            switch (propid)
            {
                case (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy:
                    ActiveIntellisenseProjectHostObject = var as IConfiguredProjectHostObject;
                    OnPropertyChanged(itemid, propid);
                    return VSConstants.S_OK;

                default:
                    return base.SetProperty(itemid, propid, var);
            }
        }

        public override int UnadviseHierarchyEvents(uint dwCookie)
        {
            _hierEventSinks.Remove(dwCookie);
            return base.UnadviseHierarchyEvents(dwCookie);
        }

        private void OnPropertyChanged(uint itemid, int propid, uint flags = 0)
        {
            if (DisposingConfiguredProjectHostObjects)
            {
                _pendingItemIds.Add(itemid);
                return;
            }

            foreach (var eventSinkKvp in _hierEventSinks)
            {
                eventSinkKvp.Value.OnPropertyChanged(itemid, propid, flags);
            }
        }

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            _hierEventSinks.Clear();
        }

        #endregion
    }
}
