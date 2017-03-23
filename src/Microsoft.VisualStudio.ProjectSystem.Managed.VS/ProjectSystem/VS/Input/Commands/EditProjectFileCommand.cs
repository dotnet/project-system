// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectCommand(CommandGroup.VisualStudioStandard2k, VisualStudioStandard2kCommandId.EditProjectFile)]
    [AppliesTo(ProjectCapability.OpenProjectFile)]
    internal class EditProjectFileCommand : AbstractProjectCommand
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly Lazy<IProjectFileEditorPresenter> _editorState;

        [ImportingConstructor]
        public EditProjectFileCommand(UnconfiguredProject unconfiguredProject, Lazy<IProjectFileEditorPresenter> editorState)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(editorState, nameof(editorState));

            _unconfiguredProject = unconfiguredProject;
            _editorState = editorState;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            // Similar to other commands such as Add Reference, Project.EditProjectFile command 
            // should be available regardless of what nodes are selected.
            return GetCommandStatusResult.Handled(GetCommandText(), CommandStatus.Enabled);
        }

        protected override async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            await _editorState.Value.OpenEditorAsync().ConfigureAwait(false);
            return true;
        }

        protected string GetCommandText()
        {
            return string.Format(VSResources.EditProjectFileCommand, Path.GetFileName(_unconfiguredProject.FullPath));
        }
    }
}
