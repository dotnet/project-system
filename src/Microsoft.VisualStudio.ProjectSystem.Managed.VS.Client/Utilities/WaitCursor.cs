// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Sets the mouse cursor to <see cref="Cursors.WaitCursor"/> within a given scope,
    /// returning the cursor to its previous icon when disposed.
    /// </summary>
    internal sealed class WaitCursor : IDisposable
    {
        private Cursor? _previousCursor;

        public WaitCursor()
        {
            _previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
        }

        public void Dispose()
        {
            if (_previousCursor != null)
            {
                Cursor.Current = _previousCursor;
                _previousCursor = null;
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }
    }
}
