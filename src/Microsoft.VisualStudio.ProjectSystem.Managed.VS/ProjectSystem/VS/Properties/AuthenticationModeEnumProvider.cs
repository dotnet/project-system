// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportDynamicEnumValuesProvider(nameof(AuthenticationModeEnumProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AuthenticationModeEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly IRemoteDebuggerAuthenticationService _remoteDebuggerAuthenticationService;

        [ImportingConstructor]
        public AuthenticationModeEnumProvider(IRemoteDebuggerAuthenticationService remoteDebuggerAuthenticationService)
        {
            _remoteDebuggerAuthenticationService = remoteDebuggerAuthenticationService;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new AuthenticationModeEnumValuesGenerator(_remoteDebuggerAuthenticationService));
        }

        private class AuthenticationModeEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly IRemoteDebuggerAuthenticationService _remoteDebuggerAuthenticationService;

            public AuthenticationModeEnumValuesGenerator(IRemoteDebuggerAuthenticationService remoteDebuggerAuthenticationService)
            {
                _remoteDebuggerAuthenticationService = remoteDebuggerAuthenticationService;
            }

            public bool AllowCustomValues => false;

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var enumValues =_remoteDebuggerAuthenticationService
                    .GetRemoteAuthenticationModes()
                    .Select(i => new PageEnumValue(new EnumValue
                    {
                        Name = i.Name,
                        DisplayName = i.DisplayName
                    }))
                    .ToArray<IEnumValue>();

                return Task.FromResult<ICollection<IEnumValue>>(enumValues);
            }

            /// <summary>
            /// The user can't add arbitrary authentication modes, so this method is unsupported.
            /// </summary>
            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();
        }
    }
}
