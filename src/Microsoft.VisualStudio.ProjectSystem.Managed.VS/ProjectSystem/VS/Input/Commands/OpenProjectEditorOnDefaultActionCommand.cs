// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    // Opens the Project file in the text editor when the user double-clicks or presses ENTER on the project file while its selected
    [ProjectCommand(CommandGroup.UIHierarchyWindow, UIHierarchyWindowCommandId.DoubleClick, UIHierarchyWindowCommandId.EnterKey)]
    [AppliesTo(ProjectCapability.DoubleClickEditProjectFile)]
    [Order(Order.Default)]
    internal class OpenProjectEditorOnDefaultActionCommand : AbstractSingleNodeProjectCommand
    {
        // comes from Microsoft.VisualStudio.ProjectSystem.VS.Implementation.ProjectFileEditorFactory
        public const string EditorFactoryGuidString = "ebc191fb-ab14-4a9f-b3a8-41093791991e";
        public static readonly Guid EditorFactoryGuid = Guid.Parse(EditorFactoryGuidString);

        private readonly IServiceProvider _serviceProvider;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public OpenProjectEditorOnDefaultActionCommand([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, UnconfiguredProject unconfiguredProject, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
            _unconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string commandText, CommandStatus progressiveStatus)
        {
            if (node.IsRoot())
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (node.IsRoot())
            {
                await _threadingService.SwitchToUIThread();

                VsShellUtilities.OpenDocumentWithSpecificEditor(_serviceProvider, _unconfiguredProject.FullPath, EditorFactoryGuid, Guid.Empty);

                return true;
            }

            return false;
        }
    }
}
