// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Provides access to debugger settings by reading the registry.
    /// </summary>
    [Export(typeof(IDebuggerSettings))]
    internal class RegistryBasedDebuggerSettings : IDebuggerSettings
    {
        private readonly IRegistry _registry;
        private readonly IVsService<SVsShell, IVsShell> _shell;
        private readonly IVsShellUtilitiesHelper _shellUtilitiesHelper;

        [ImportingConstructor]
        public RegistryBasedDebuggerSettings(
            IRegistry registry,
            IVsService<SVsShell, IVsShell> shell,
            IVsShellUtilitiesHelper shellUtilitiesHelper)
        {
            _registry = registry;
            _shell = shell;
            _shellUtilitiesHelper = shellUtilitiesHelper;
        }

        public async Task<bool> IsEncEnabledAsync()
        {
            string? registryRoot = await _shellUtilitiesHelper.GetRegistryRootAsync(_shell);
            if (registryRoot is not null
                && _registry.ReadValueForCurrentUser(registryRoot + @"\Debugger", "ENCEnable") is int value)
            {
                return value != 0;
            }

            return true;
        }

        public async Task<bool> IsNonDebugHotReloadEnabledAsync()
        {
            string? registryRoot = await _shellUtilitiesHelper.GetRegistryRootAsync(_shell);
            if (registryRoot is not null
                && _registry.ReadValueForCurrentUser(registryRoot + @"\Debugger", "EnableNetHotReloadWhenNoDebugging") is int value)
            {
                return value != 0;
            }

            return true;
        }
    }
}
