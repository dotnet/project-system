// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
                    progressiveStatus |= CommandStatus.InvisibleOnContextMenu;
                }

                return new CommandStatusResult(handled: true, commandText, progressiveStatus);
            }

            return CommandStatusResult.Unhandled;
        }

        public bool TryHandleCommand(IImmutableSet<IProjectTree> items, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            string? identifier = GetReferenceProviderIdentifier(commandId);

            if (identifier is not null)
            {
                _referencesUI.ShowReferenceManagerDialog(new Guid(identifier));
                return true;
            }

            return false;
        }

        private bool CanAddReference(long commandId)
        {
            string? identifier = GetReferenceProviderIdentifier(commandId);
            if (identifier is not null)
            {
                Lazy<IVsReferenceManagerUserAsync>? user = ReferenceManagerUsers.FirstOrDefault(u => u.Metadata.ProviderContextIdentifier == identifier);

                return user?.Value.IsApplicable() == true;
            }

            return false;
        }

        private static string? GetReferenceProviderIdentifier(long commandId)
        {
            return (VSConstants.VSStd16CmdID)commandId switch
            {
                VSConstants.VSStd16CmdID.AddAssemblyReference => VSConstants.AssemblyReferenceProvider_string,
                VSConstants.VSStd16CmdID.AddComReference => VSConstants.ComReferenceProvider_string,
                VSConstants.VSStd16CmdID.AddProjectReference => VSConstants.ProjectReferenceProvider_string,
                VSConstants.VSStd16CmdID.AddSharedProjectReference => VSConstants.SharedProjectReferenceProvider_string,
                VSConstants.VSStd16CmdID.AddSdkReference => VSConstants.PlatformReferenceProvider_string,
                _ => null,
            };

            // Other known provider GUIDs:
            //
            // - VSConstants.FileReferenceProvider_string
            // - VSConstants.ConnectedServiceInstanceReferenceProvider_string
        }
    }
}
