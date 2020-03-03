// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.References;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Enables commands for launching Reference Manager at a specific page.
    /// </summary>
    [ExportCommandGroup(VSConstants.CMDSETID.StandardCommandSet16_string)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(ProjectSystem.Order.Default)]
    internal sealed class ReferenceManagerCommandHandler : ICommandGroupHandler
    {
        public const int CmdidAddAssemblyReference       = 0x200;
        public const int CmdidAddComReference            = 0x201;
        public const int CmdidAddProjectReference        = 0x202;
        public const int CmdidAddSharedProjectReference  = 0x203;
        public const int CmdidAddSdkReference          = 0x204;

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IReferencesUI _referencesUI;

        [ImportingConstructor]
        public ReferenceManagerCommandHandler(UnconfiguredProject unconfiguredProject, IReferencesUI referencesUI)
        {
            _unconfiguredProject = unconfiguredProject;
            _referencesUI = referencesUI;
        }

        public CommandStatusResult GetCommandStatus(IImmutableSet<IProjectTree> items, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            bool enable = commandId switch
            {
                CmdidAddAssemblyReference      => _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.AssemblyReferences),
                CmdidAddComReference           => _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.ComReferences),
                CmdidAddProjectReference       => _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.ProjectReferences),
                CmdidAddSharedProjectReference => _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.SharedProjectReferences),
                CmdidAddSdkReference           => _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.WinRTReferences) && _unconfiguredProject.Capabilities.AppliesTo(ProjectCapabilities.SdkReferences),
                _ => false
            };

            if (enable)
            {
                progressiveStatus &= ~CommandStatus.Invisible;
                progressiveStatus |= CommandStatus.Enabled | CommandStatus.Supported;
                return new CommandStatusResult(handled: true, commandText, progressiveStatus);
            }

            return CommandStatusResult.Unhandled;
        }

        public bool TryHandleCommand(IImmutableSet<IProjectTree> items, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Guid guid = commandId switch
            {
                CmdidAddAssemblyReference      => VSConstants.AssemblyReferenceProvider_Guid,
                CmdidAddComReference           => VSConstants.ComReferenceProvider_Guid,
                CmdidAddProjectReference       => VSConstants.ProjectReferenceProvider_Guid,
                CmdidAddSharedProjectReference => VSConstants.SharedProjectReferenceProvider_Guid,
                CmdidAddSdkReference           => VSConstants.PlatformReferenceProvider_Guid,
                _ => Guid.Empty
            };

            // Other known provider GUIDs:
            //
            // - VSConstants.FileReferenceProvider_Guid
            // - VSConstants.ConnectedServiceInstanceReferenceProvider_Guid

            if (guid != Guid.Empty)
            {
                _referencesUI.ShowReferenceManagerDialog(guid);
                return true;
            }

            return false;
        }
    }
}
