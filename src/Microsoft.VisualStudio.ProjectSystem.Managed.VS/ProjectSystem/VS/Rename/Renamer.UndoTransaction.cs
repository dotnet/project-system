// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal sealed partial class Renamer
    {
        private sealed class UndoScope : IDisposable
        {
            private readonly string _renameOperationName;
            private readonly DTE _dte;
            private bool _shouldClose = true;

            private UndoScope(string renameOperationName, DTE dte)
            {
                _renameOperationName = renameOperationName;
                _dte = dte;
            }

            internal static UndoScope Create(DTE dte, string renameOperationName)
            {
                var undo = new UndoScope(renameOperationName, dte);
                undo.StartUndo();
                return undo;
            }

            private void StartUndo()
            {
                if (_dte.UndoContext.IsOpen)
                {
                    _shouldClose = false;
                }

                _dte.UndoContext.Open(_renameOperationName, false);
            }

            public void Dispose()
            {
                if (_shouldClose)
                {
                    _dte.UndoContext.Close();
                }
            }
        }
    }
}
