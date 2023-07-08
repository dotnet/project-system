// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Registers VS menu commands provided by the managed language project system package in.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class PackageCommandRegistrationService : IPackageService
    {
        /// <summary>
        /// <see cref="MenuCommand"/> implementations may export themselves with this contract name
        /// to be automatically added when the managed-language project system package initializes.
        /// </summary>
        public const string PackageCommandContract = "ManagedPackageCommand";

        private readonly JoinableTaskContext _context;
        private readonly IEnumerable<MenuCommand> _commands;

        [ImportingConstructor]
        public PackageCommandRegistrationService([ImportMany(PackageCommandContract)] IEnumerable<MenuCommand> commands, JoinableTaskContext context)
        {
            _commands = commands;
            _context = context;
        }

        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            _context.VerifyIsOnMainThread();

            IMenuCommandService menuCommandService = await asyncServiceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>();

            foreach (MenuCommand menuCommand in _commands)
            {
                menuCommandService.AddCommand(menuCommand);
            }
        }
    }
}
