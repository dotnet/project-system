// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal class EditProjectFileCommand : AbstractSingleNodeProjectCommand
    {
        private static readonly Guid XmlEditorFactoryGuid = new Guid("{fa3cd31e-987b-443a-9b81-186104e8dac1}");
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

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(node), CommandStatus.Enabled) :
                GetCommandStatusResult.Unhandled;

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node)) return false;
            await _editorState.Value.OpenEditorAsync().ConfigureAwait(false);
            return true;
        }

        protected string GetCommandText(IProjectTree node)
        {
            return string.Format(VSResources.EditProjectFileCommand, Path.GetFileName(_unconfiguredProject.FullPath));
        }

        private bool ShouldHandle(IProjectTree node) => node.IsRoot();
    }
}
