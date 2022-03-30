// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Provides support for all Add Item commands that operate on <see cref="IProjectTree"/> nodes, across C# and VB
    /// </summary>
    internal abstract partial class AbstractAddItemCommandHandler : IAsyncCommandGroupHandler
    {
        protected static readonly Guid LegacyCSharpPackageGuid = new("{FAE04EC1-301F-11d3-BF4B-00C04F79EFBC}");
        protected static readonly Guid LegacyVBPackageGuid = new("{164B10B9-B200-11d0-8C61-00A0C91E29D5}");
        private readonly ConfiguredProject _configuredProject;
        private readonly IAddItemDialogService _addItemDialogService;
        private readonly IVsUIService<IVsShell> _vsShell;

        protected AbstractAddItemCommandHandler(ConfiguredProject configuredProject, IAddItemDialogService addItemDialogService, IVsUIService<SVsShell, IVsShell> vsShell)
        {
            _configuredProject = configuredProject;
            _addItemDialogService = addItemDialogService;
            _vsShell = vsShell;
        }

        /// <summary>
        /// Gets the list of potential templates that could apply to this handler. Implementors should cache the results of this method.
        /// </summary>
        protected abstract ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails();

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (nodes.Count == 1 && _addItemDialogService.CanAddNewOrExistingItemTo(nodes.First()) && TryGetTemplateDetails(commandId, out _))
            {
                return GetCommandStatusResult.Handled(commandText, CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            Requires.NotNull(nodes, nameof(nodes));

            if (nodes.Count == 1 && _addItemDialogService.CanAddNewOrExistingItemTo(nodes.First()) && TryGetTemplateDetails(commandId, out TemplateDetails? result))
            {
                IVsShell vsShell = _vsShell.Value;

                // Look up the resources from each package to get the strings to pass to the Add Item dialog.
                // These strings must match what is used in the template exactly, including localized versions. Rather than relying on
                // our localizations being the same as the VS repository localizations we just load the right strings using the same
                // resource IDs as the templates themselves use.
                string localizedDirectoryName = vsShell.LoadPackageString(result.DirNamePackageGuid, result.DirNameResourceId);
                string localizedTemplateName = vsShell.LoadPackageString(result.TemplateNamePackageGuid, result.TemplateNameResourceId);

                await _addItemDialogService.ShowAddNewItemDialogAsync(nodes.First(), localizedDirectoryName, localizedTemplateName);
                return true;
            }

            return false;
        }

        private bool TryGetTemplateDetails(long commandId, [NotNullWhen(returnValue: true)] out TemplateDetails? result)
        {
            IProjectCapabilitiesScope capabilities = _configuredProject.Capabilities;

            if (GetTemplateDetails().TryGetValue(commandId, out ImmutableArray<TemplateDetails> templates))
            {
                foreach (TemplateDetails template in templates)
                {
                    if (capabilities.AppliesTo(template.AppliesTo))
                    {
                        result = template;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
