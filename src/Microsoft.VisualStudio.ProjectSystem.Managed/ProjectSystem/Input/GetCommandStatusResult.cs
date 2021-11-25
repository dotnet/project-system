// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Input
{
    internal static class GetCommandStatusResult
    {
        public static Task<CommandStatusResult> Unhandled { get; } = CommandStatusResult.Unhandled.AsTask();

        public static Task<CommandStatusResult> Suppressed { get; } = Task.FromResult(new CommandStatusResult(handled: true, null, CommandStatus.NotSupported | CommandStatus.Invisible));

        public static Task<CommandStatusResult> Handled(string? commandText, CommandStatus progressiveStatus)
        {
            return new CommandStatusResult(true, commandText, progressiveStatus | CommandStatus.Supported).AsTask();
        }
    }
}
