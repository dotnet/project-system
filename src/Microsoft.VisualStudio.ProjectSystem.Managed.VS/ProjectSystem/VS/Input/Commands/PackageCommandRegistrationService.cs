// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Registers VS menu commands provided by the managed language project system package in.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class PackageCommandRegistrationService : IPackageService
    {
        /// <inheritdoc />
        public async Task<IDisposable?> InitializeAsync(ManagedProjectSystemPackage package, IComponentModel componentModel)
        {
            OleMenuCommandService mcs = await package.GetServiceAsync<IMenuCommandService, OleMenuCommandService>();

            mcs.AddCommand(componentModel.GetService<DebugFrameworksDynamicMenuCommand>());
            mcs.AddCommand(componentModel.GetService<DebugFrameworkPropertyMenuTextUpdater>());

            return null;
        }
    }
}
