// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    internal abstract class BaseVersionValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string PackageVersionMSBuildProperty = "Version";
        protected static readonly Version DefaultVersion = new Version(1, 0, 0);

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

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
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

            return await base.OnSetPropertyValueAsync(unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
        }
    }
}
