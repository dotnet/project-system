// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
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
                { "dll",             new PageEnumValue(new EnumValue {Name = "dll",    DisplayName = "0" }) },
                { "exe",             new PageEnumValue(new EnumValue {Name = "exe",    DisplayName = "1" }) },
                { "winexe",          new PageEnumValue(new EnumValue {Name = "winexe", DisplayName = "2" }) }
            };

            private readonly Dictionary<string, IEnumValue> _mappedOutputTypeValues = new Dictionary<string, IEnumValue>
            {
                { "winmdobj",        new PageEnumValue(new EnumValue {Name = "dll",    DisplayName = "0" }) },
                { "appcontainerexe", new PageEnumValue(new EnumValue {Name = "exe",    DisplayName = "1" }) }
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
                IEnumValue value;
                if (_listedOutputTypeValues.TryGetValue(userSuppliedValue, out value))
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
