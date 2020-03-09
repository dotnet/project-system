// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;

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
        [ImportMany]
        public OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> UIProviders { get; }

        [ImportingConstructor]
        public LaunchTargetEnumProvider(UnconfiguredProject project)
        {
            UIProviders = new OrderPrecedenceImportCollection<ILaunchSettingsUIProvider>(projectCapabilityCheckProvider: project);
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new LaunchTargetEnumValuesGenerator(UIProviders));
        }

        internal class LaunchTargetEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> _uiProviders;

            public LaunchTargetEnumValuesGenerator(OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> uIProviders)
            {
                _uiProviders = uIProviders;
            }

            public bool AllowCustomValues => false;

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
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

                return Task.FromResult<ICollection<IEnumValue>>(enumValues.ToArray<IEnumValue>());
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
