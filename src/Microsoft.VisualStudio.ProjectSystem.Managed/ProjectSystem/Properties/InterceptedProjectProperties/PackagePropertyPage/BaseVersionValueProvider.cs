// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    internal abstract class BaseVersionValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string PackageVersionMSBuildProperty = "Version";
        protected static readonly Version DefaultVersion = new(1, 0, 0);

        protected abstract string PropertyName { get; }

        protected virtual async Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            string versionStr = await defaultProperties.GetEvaluatedPropertyValueAsync(PackageVersionMSBuildProperty);
            if (string.IsNullOrEmpty(versionStr))
            {
                return DefaultVersion;
            }

            // Ignore the semantic version suffix (e.g. "1.0.0-beta1" => "1.0.0")
            versionStr = new LazyStringSplit(versionStr, '-').First();

            return Version.TryParse(versionStr, out Version version) ? version : DefaultVersion;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            // Default value is Version (major.minor.build components only)
            Version version = await GetDefaultVersionAsync(defaultProperties);
            return version.ToString();
        }

        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            // Don't set the new value if both of the following is true:
            //  1. There is no existing property entry AND
            //  2. The new value is identical to the default value.

            IEnumerable<string> propertyNames = await defaultProperties.GetPropertyNamesAsync();
            if (!propertyNames.Contains(PropertyName))
            {
                if (Version.TryParse(unevaluatedPropertyValue, out Version version))
                {
                    Version defaultVersion = await GetDefaultVersionAsync(defaultProperties);
                    if (version.Equals(defaultVersion))
                    {
                        return null;
                    }
                }
            }

            return unevaluatedPropertyValue;
        }
    }
}
