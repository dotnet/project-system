// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.References;

using VsReferenceManagerUserImportCollection = Microsoft.VisualStudio.ProjectSystem.OrderPrecedenceImportCollection<Microsoft.VisualStudio.ProjectSystem.VS.References.IVsReferenceManagerUserAsync, Microsoft.VisualStudio.ProjectSystem.VS.References.IVsReferenceManagerUserComponentMetadataView>;

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
        // WORKAROUND: Until we can consume the new CommandStatus from CPS
        public const CommandStatus InvisibleOnContextMenu = (CommandStatus)unchecked((int)0x20);

        public const int CmdidAddAssemblyReference       = 0x200;
        public const int CmdidAddComReference            = 0x201;
        public const int CmdidAddProjectReference        = 0x202;
        public const int CmdidAddSharedProjectReference  = 0x203;
        public const int CmdidAddSdkReference          = 0x204;

        private readonly IReferencesUI _referencesUI;

        [ImportingConstructor]
        public ReferenceManagerCommandHandler(ConfiguredProject project, IReferencesUI referencesUI)
        {
            _referencesUI = referencesUI;
            ReferenceManagerUsers = new VsReferenceManagerUserImportCollection(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public VsReferenceManagerUserImportCollection ReferenceManagerUsers
        {
            get;
        }

        public CommandStatusResult GetCommandStatus(IImmutableSet<IProjectTree> items, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (CanAddReference(commandId))
            {
                progressiveStatus &= ~CommandStatus.Invisible;
                progressiveStatus |= CommandStatus.Enabled | CommandStatus.Supported;

                if (items.Any(tree => tree.IsFolder))
                {   // Hide these commands for Folder -> Add

                    progressiveStatus |= InvisibleOnContextMenu;
                }

                return new CommandStatusResult(handled: true, commandText, progressiveStatus);
            }

            return CommandStatusResult.Unhandled;
        }

        public bool TryHandleCommand(IImmutableSet<IProjectTree> items, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            string? identifier = GetReferenceProviderIdentifier(commandId);

            if (identifier != null)
            {
                _referencesUI.ShowReferenceManagerDialog(new Guid(identifier));
                return true;
            }

            return false;
        }

        private bool CanAddReference(long commandId)
        {
            string? identifier = GetReferenceProviderIdentifier(commandId);
            if (identifier != null)
            {
                Lazy<IVsReferenceManagerUserAsync> user = ReferenceManagerUsers.FirstOrDefault(u => u.Metadata.ProviderContextIdentifier == identifier);

                return user != null && user.Value.IsApplicable(); 
            }

            return false;
        }

        private static string? GetReferenceProviderIdentifier(long commandId)
        {
            return commandId switch
            {
                CmdidAddAssemblyReference => VSConstants.AssemblyReferenceProvider_string,
                CmdidAddComReference => VSConstants.ComReferenceProvider_string,
                CmdidAddProjectReference => VSConstants.ProjectReferenceProvider_string,
                CmdidAddSharedProjectReference => VSConstants.SharedProjectReferenceProvider_string,
                CmdidAddSdkReference => VSConstants.PlatformReferenceProvider_string,
                _ => null,
            };

            // Other known provider GUIDs:
            //
            // - VSConstants.FileReferenceProvider_string
            // - VSConstants.ConnectedServiceInstanceReferenceProvider_string
        }
    }
}
