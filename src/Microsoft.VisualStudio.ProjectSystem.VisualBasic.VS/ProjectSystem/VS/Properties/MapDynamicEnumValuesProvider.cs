// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// This Dynamic Enum Value Provider maps enum based msbuild property value
    /// to values that we want to display in the UI and also map the value,we 
    /// obtain from the UI to msbuild complaint values for persistence
    /// </summary>
    internal class MapDynamicEnumValuesProvider : IDynamicEnumValuesGenerator
    {
        private readonly IDictionary<string, IEnumValue> _getValueMap;
        private readonly IDictionary<string, IEnumValue> _setValueMap;

        public MapDynamicEnumValuesProvider(
            IDictionary<string, IEnumValue> getValueMap,
            IDictionary<string, IEnumValue> setValueMap)
        {
            Requires.NotNull(getValueMap, nameof(getValueMap));
            Requires.NotNull(setValueMap, nameof(setValueMap));

            _getValueMap = getValueMap;
            _setValueMap = setValueMap;
        }

        public bool AllowCustomValues => false;

        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            return Task.FromResult(_getValueMap.Values);
        }

        public Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            if (_setValueMap != null)
            {
                if (_setValueMap.TryGetValue(userSuppliedValue, out IEnumValue value))
                {
                    return Task.FromResult(value);
                }
            }

            return Task.FromResult<IEnumValue>(null);
        }
    }
}