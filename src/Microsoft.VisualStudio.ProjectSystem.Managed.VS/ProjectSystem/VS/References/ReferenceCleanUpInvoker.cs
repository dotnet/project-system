// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class ReferenceCleanUpInvoker
    {
        private readonly Stack<IReferenceCommand> _undoStack = new Stack<IReferenceCommand>();
        private readonly Stack<IReferenceCommand> _redoStack = new Stack<IReferenceCommand>();

        public async Task ExecuteCommandAsync(IReferenceCommand command)
        {
            // Clear redo stack
            _redoStack.Clear();
            await command.Execute();
            _undoStack.Push(command);
        }

        public async Task UndoCommand()
        {
            if (_undoStack.Count <= 0)
            {
                return;
            }

            IReferenceCommand cmd = _undoStack.Peek();
            await cmd.Undo();
            _redoStack.Push(cmd);
            _undoStack.Pop();
        }

        public async Task RedoCommand()
        {
            if (_redoStack.Count <= 0)
            {
                return;
            }

            IReferenceCommand cmd = _redoStack.Peek();
            await cmd.Redo();
            _undoStack.Push(cmd);
            _redoStack.Pop();
        }
    }
}
