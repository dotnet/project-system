// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    /// <summary>
    /// This Dynamic Enum Value Provider provides enum based msbuild property
    /// value to display in the UI and also map the value, we obtain from the
    /// UI to msbuild complaint values for persistence
    /// </summary>
    internal class MapDynamicEnumValuesProvider : IDynamicEnumValuesGenerator
    {
        private readonly IDictionary<string, IEnumValue> _valueMap;
        private readonly ICollection<IEnumValue>? _getValues;

        public MapDynamicEnumValuesProvider(
            IDictionary<string, IEnumValue> valueMap,
            ICollection<IEnumValue>? getValues = null)
        {
            Requires.NotNull(valueMap, nameof(valueMap));

            _valueMap = valueMap;
            _getValues = getValues;
        }

        public bool AllowCustomValues => false;

        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return Task.FromResult(_getValues ?? _valueMap.Values);
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            if (_valueMap.TryGetValue(userSuppliedValue, out IEnumValue? value))
            {
                return Task.FromResult<IEnumValue?>(value);
            }

            return Task.FromResult<IEnumValue?>(null);
        }
    }
}
