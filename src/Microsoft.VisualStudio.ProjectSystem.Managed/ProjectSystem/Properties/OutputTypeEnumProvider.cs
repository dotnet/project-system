// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Provides the mapping between OutputType msbuild property (exposed in the browse object through OutputTypeEx)
    /// and OutputType property on the BrowseObject.
    /// Winmdobj and appcontainerexe need to be mapped to dll and exe respectively for the OutputTypeproperty.
    /// </summary>
    [ExportDynamicEnumValuesProvider("OutputTypeEnumProvider")]
    [AppliesTo(ProjectCapability.Managed)]
    internal class OutputTypeEnumProvider : IDynamicEnumValuesProvider
    {
        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new OutputTypeEnumValuesGenerator());
        }

        internal class OutputTypeEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly Dictionary<string, IEnumValue> _listedOutputTypeValues = new Dictionary<string, IEnumValue>
            {
                { "WinExe",          new PageEnumValue(new EnumValue {Name = "WinExe",  DisplayName = "0" }) },
                { "Exe",             new PageEnumValue(new EnumValue {Name = "Exe",     DisplayName = "1" }) },
                { "Library",         new PageEnumValue(new EnumValue {Name = "Library", DisplayName = "2" }) },
            };

            private readonly Dictionary<string, IEnumValue> _mappedOutputTypeValues = new Dictionary<string, IEnumValue>
            {
                { "WinMDObj",        new PageEnumValue(new EnumValue {Name = "Library", DisplayName = "2" }) },
                { "AppContainerExe", new PageEnumValue(new EnumValue {Name = "Exe",     DisplayName = "1" }) }
            };

            public bool AllowCustomValues => false;

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                // CPS doesn't like it if we have duplicate values in the list of possible values. So take just
                // the first three which are not duplicates.
                return Task.FromResult<ICollection<IEnumValue>>(_listedOutputTypeValues.Values);
            }

            public Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
            {
                if (_listedOutputTypeValues.TryGetValue(userSuppliedValue, out IEnumValue value))
                {
                    return Task.FromResult(value);
                }

                if (_mappedOutputTypeValues.TryGetValue(userSuppliedValue, out value))
                {
                    return Task.FromResult(value);
                }

                return Task.FromResult<IEnumValue>(null);
            }
        }
    }
}
