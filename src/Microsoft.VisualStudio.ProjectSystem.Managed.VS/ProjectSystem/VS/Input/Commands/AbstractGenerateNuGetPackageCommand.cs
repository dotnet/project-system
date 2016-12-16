// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Input;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractGenerateNuGetPackageCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IProjectThreadingService _threadingService;

        protected AbstractGenerateNuGetPackageCommand(UnconfiguredProject unconfiguredProject, IProjectThreadingService threadingService)
        {
            UnconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
        }

        protected UnconfiguredProject UnconfiguredProject { get; }

        protected abstract string GetCommandText();
        protected abstract bool ShouldHandle(IProjectTree node);

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(), CommandStatus.Enabled) :
                GetCommandStatusResult.Unhandled;

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, Int64 commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node)) return false;

            await _threadingService.SwitchToUIThread();

            // TODO: Generate NuGet package.

            return true;
        }
    }
}
