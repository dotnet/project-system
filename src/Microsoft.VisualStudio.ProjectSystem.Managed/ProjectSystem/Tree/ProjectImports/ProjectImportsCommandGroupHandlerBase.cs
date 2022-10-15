// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.ProjectImports;

/// <summary>
/// Handles opening of files displayed in the project imports tree.
/// </summary>
internal abstract class ProjectImportsCommandGroupHandlerBase : IAsyncCommandGroupHandler
{
    protected ProjectImportsCommandGroupHandlerBase()
    {
    }

    protected abstract bool IsOpenCommand(long commandId);

    protected abstract bool IsOpenWithCommand(long commandId);

    protected abstract void OpenItems(long commandId, IImmutableSet<IProjectTree> items);

    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> items, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (IsOpenCommand(commandId) && items.All(CanOpenFile))
        {
            progressiveStatus |= CommandStatus.Enabled | CommandStatus.Supported;
            return new CommandStatusResult(true, commandText, progressiveStatus).AsTask();
        }

        return CommandStatusResult.Unhandled.AsTask();
    }

    public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> items, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (IsOpenCommand(commandId) && items.All(CanOpenFile))
        {
            OpenItems(commandId, items);

            return TaskResult.True;
        }

        return TaskResult.False;
    }

    internal static bool CanOpenFile(IProjectTree node) => node.Flags.Contains(ImportTreeProvider.ProjectImport);

    /// <summary>
    /// Calls <paramref name="action"/> for each of <paramref name="items"/>. If any action
    /// throws, its exception is caught and processing continues. When all items have been
    /// handled, any exceptions are thrown either as a single exception or an
    /// <see cref="AggregateException"/>.
    /// </summary>
    internal static void RunAllAndAggregateExceptions<T>(IEnumerable<T> items, Action<T> action)
    {
        List<Exception>? exceptions = null;

        foreach (T item in items)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        if (exceptions is not null)
        {
            if (exceptions.Count == 1)
            {
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
            }
            else
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
