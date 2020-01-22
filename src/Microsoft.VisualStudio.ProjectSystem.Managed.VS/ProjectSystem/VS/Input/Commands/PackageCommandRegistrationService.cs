// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

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
            Assumes.True(_context.IsOnMainThread, "Must be on UI thread");

            IMenuCommandService menuCommandService = await asyncServiceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>();

            foreach (MenuCommand menuCommand in _commands)
            {
                menuCommandService.AddCommand(menuCommand);
            }
        }
    }
}
