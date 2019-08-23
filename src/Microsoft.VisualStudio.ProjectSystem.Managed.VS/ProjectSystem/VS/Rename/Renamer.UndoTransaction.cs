// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal sealed partial class Renamer
    {
        private sealed class UndoScope : IDisposable
        {
            private readonly string _renameOperationName;
            private readonly IVsService<DTE> _dte;
            private readonly IProjectThreadingService _threadingService;
            private bool _shouldClose = true;

            private UndoScope(string renameOperationName, IVsService<DTE> dte, IProjectThreadingService threadingService)
            {
                _renameOperationName = renameOperationName;
                _dte = dte;
                _threadingService = threadingService;
            }

            internal static async Task<UndoScope> CreateAsync(IVsService<DTE> dte,
                                                                    IProjectThreadingService threadingService,
                                                                    string renameOperationName,
                                                                    CancellationToken token = default)
            {
                var undo = new UndoScope(renameOperationName, dte, threadingService);
                await undo.StartUndoAsync(token);
                return undo;
            }

            private async Task StartUndoAsync(CancellationToken token = default)
            {
                DTE? dte = await _dte.GetValueAsync(token);
                if (dte!.UndoContext.IsOpen)
                {
                    _shouldClose = false;
                }
                dte.UndoContext.Open(_renameOperationName, false);
            }

            public void Dispose()
            {
                if (_shouldClose)
                {
                    _threadingService.ExecuteSynchronously(async () =>
                    {
                        DTE? dte = await _dte.GetValueAsync();
                        dte!.UndoContext.Close();
                    });
                }
            }
        }
    }
}
