// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsOutputWindowFactory
    {
        public static IVsOutputWindow Create()
        {
            return new VsOutputWindowMock();
        }

        private class VsOutputWindowMock : IVsOutputWindow
        {
            private readonly Dictionary<Guid, IVsOutputWindowPane> _panes = new();

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
        }
    }
}
