// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Returns the set of supported launch targets for the project based on the
    /// available <see cref="ILaunchSettingsUIProvider"/>s.
    /// </summary>
    [ExportDynamicEnumValuesProvider("LaunchTargetEnumProvider")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchTargetEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportMany]
        public OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> UIProviders { get; }

        [ImportingConstructor]
        public LaunchTargetEnumProvider(UnconfiguredProject project, JoinableTaskContext joinableTaskContext)
        {
            _joinableTaskContext = joinableTaskContext;
            UIProviders = new OrderPrecedenceImportCollection<ILaunchSettingsUIProvider>(projectCapabilityCheckProvider: project);
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new LaunchTargetEnumValuesGenerator(UIProviders, _joinableTaskContext));
        }

        internal class LaunchTargetEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly JoinableTaskContext _joinableTaskContext;
            private readonly OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> _uiProviders;

            public LaunchTargetEnumValuesGenerator(
                OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> uiProviders,
                JoinableTaskContext joinableTaskContext)
            {
                _joinableTaskContext = joinableTaskContext;
                _uiProviders = uiProviders;
            }

            public bool AllowCustomValues => false;

            public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                // Some ILaunchSettingsUIProviders depend on being created on the UI thread, so we
                // need to switch before we access them.
                await _joinableTaskContext.Factory.SwitchToMainThreadAsync();

                // There may be providers with duplicate command names. We'll just use the first one
                // we come across for any given command name.
                var enumValues = new List<PageEnumValue>();
                foreach (Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView> provider in _uiProviders)
                {
                    if (enumValues.FirstOrDefault(launchType => launchType.Name.Equals(provider.Value.CommandName)) == null)
                    {
                        enumValues.Add(new PageEnumValue(new EnumValue
                        {
                            Name = provider.Value.CommandName,
                            DisplayName = provider.Value.FriendlyName
                        }));
                    }
                }

                return enumValues.ToArray<IEnumValue>();
            }

            /// <summary>
            /// This provider should only be used to get values, and there shouldn't be any way
            /// for the user to create a new value, so this method should never be called.
            /// </summary>
            /// <param name="userSuppliedValue"></param>
            /// <returns></returns>
            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
