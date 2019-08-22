// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsOutputWindow2Factory
    {
        public static IVsOutputWindow CreateWithActivePane(IVsOutputWindowPane pane)
        {
            return new VsOutputWindowMock(pane);
        }

        private class VsOutputWindowMock : IVsOutputWindow, IVsOutputWindow2
        {
            private readonly Guid _activePaneGuid = Guid.NewGuid();
            private readonly Dictionary<Guid, IVsOutputWindowPane> _panes = new Dictionary<Guid, IVsOutputWindowPane>();

            public VsOutputWindowMock(IVsOutputWindowPane activePane)
            {
                _panes.Add(_activePaneGuid, activePane);
            }

            public int GetPane(ref Guid rguidPane, out IVsOutputWindowPane ppPane)
            {
                _panes.TryGetValue(rguidPane, out ppPane);

                return 0;
            }

            public int CreatePane(ref Guid rguidPane, string pszPaneName, int fInitVisible, int fClearWithSolution)
            {
                _panes[rguidPane] = IVsOutputWindowPaneFactory.Create();

                return 0;
            }

            public int DeletePane(ref Guid rguidPane)
            {
                throw new NotImplementedException();
            }

            public int GetActivePaneGUID(out Guid pguidPane)
            {
                pguidPane = _activePaneGuid;
                return 0;
            }
        }
    }
}
