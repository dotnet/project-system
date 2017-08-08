// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class BuildLogSearchTask : VsSearchTask
    {
        private readonly IWpfTableControl _control;

        public BuildLogSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, IWpfTableControl control)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            _control = control;
        }

        protected override void OnStartSearch()
        {
            _control.SetFilter(BuildLoggingToolWindow.SearchFilterKey, new BuildLogSearchFilter(SearchQuery, _control));
            SearchCallback.ReportComplete(this, dwResultsFound: 0);
        }

        protected override void OnStopSearch()
        {
            _control.SetFilter(BuildLoggingToolWindow.SearchFilterKey, null);
        }
    }
}
