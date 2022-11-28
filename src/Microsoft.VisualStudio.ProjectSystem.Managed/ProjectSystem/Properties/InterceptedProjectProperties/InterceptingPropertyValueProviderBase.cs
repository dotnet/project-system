// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Base intercepting project property provider that intercepts all the callbacks for a specific property name
    /// on the default <see cref="IProjectPropertiesProvider"/> for validation and/or transformation of the property value.
    /// </summary>
    public abstract class InterceptingPropertyValueProviderBase : IInterceptingPropertyValueProvider2
    {
        public virtual Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(evaluatedPropertyValue);
        }

        public virtual Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(unevaluatedPropertyValue);
        }

        public virtual Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return Task.FromResult<string?>(unevaluatedPropertyValue);
        }

        public virtual Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, new[]{ propertyName });
        }

        internal static async Task<bool> IsValueDefinedInContextMSBuildPropertiesAsync(IProjectProperties defaultProperties, string[] msBuildPropertyNames)
        {
            string[] propertiesDefinedInProjectFile = (await defaultProperties.GetDirectPropertyNamesAsync()).ToArray();
            return !msBuildPropertyNames.Any(static (name, properties) => properties.Contains(name, StringComparers.PropertyNames), propertiesDefinedInProjectFile);
        }
    }
}
