// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [ProjectSystemTrait]
    internal class TestPropertyProviderBase 
    {
        private Dictionary<string, Dictionary<string, object>>  _properties = new Dictionary<string,Dictionary<string,object>>();
        internal Dictionary<string, Dictionary<string, object>> Properties
        {
            get { return _properties; }
        }

        protected Dictionary<string, object> GetPropertiesForRule(string schema)
        {
            return Properties[schema];
        }

        public Task<string> GetEvaluatedPropertyValueAsync(string name)
        {
            return GetEvaluatedPropertyValueAsync(ConfigurationGeneral.SchemaName, name);
        }

        public async Task<string> GetEvaluatedPropertyValueAsync(string schema, string name)
        {
            var properties = GetPropertiesForRule(schema);
            properties.TryGetValue(name, out object value);
            if (value is IProperty)
            {
                value = await ((IProperty)value).GetValueAsync();
            }

            return value != null ? value.ToString() : string.Empty;
        }

        public virtual Task SetPropertyValueAsync(string name, object value)
        {
            return SetPropertyValueAsync(ConfigurationGeneral.SchemaName, name, value);
        }

        public virtual Task SetPropertyValueAsync(string schema, string name, object value)
        {
            var properties = GetPropertiesForRule(schema);
            return Task.Run(() =>
            {
                if (properties.ContainsKey(name))
                {
                    if (properties[name] is IProperty property)
                    {
                        property.SetValueAsync(value);
                    }
                    else
                    {
                        properties[name] = value;
                    }
                }
                else
                {
                    properties.Add(name, value);
                }
            });
        }

        public virtual Task<IProperty> GetPropertyAsync(string name)
        {
            var properties = GetPropertiesForRule(ConfigurationGeneral.SchemaName);
            return Task.FromResult(properties[name] as IProperty);
        }

        public virtual Task DeletePropertyAsync(string schema, string name)
        {
            var properties = GetPropertiesForRule(schema);
            return Task.Run(() =>
            {
                if (properties != null)
                {
                    properties.Remove(name);
                }
            });
        }
    }

    internal class TestUnconfiguredPropertyProvider : TestPropertyProviderBase
    {
    }

    internal class TestConfiguredPropertyProvider : TestPropertyProviderBase
    {
    }
}
