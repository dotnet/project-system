// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    internal abstract class BaseVersionValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string PackageVersionMSBuildProperty = "PackageVersion";
        protected static readonly Version s_DefaultVersion = new Version(1, 0, 0);

        protected abstract string PropertyName { get; }

        protected virtual async Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            var versionStr = await defaultProperties.GetEvaluatedPropertyValueAsync(PackageVersionMSBuildProperty).ConfigureAwait(true);
            if (string.IsNullOrEmpty(versionStr))
            {
                return s_DefaultVersion;
            }

            // Ignore the semantic version suffix (e.g. "1.0.0-beta1" => "1.0.0")
            versionStr = versionStr.Split('-')[0];
            return Version.TryParse(versionStr, out Version version) ? version : s_DefaultVersion;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            // Default value is Version (major.minor.build components only)
            var version = await GetDefaultVersionAsync(defaultProperties).ConfigureAwait(true);
            return version.ToString();
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            // Don't set the new value if both of the following is true:
            //  1. There is no existing property entry AND
            //  2. The new value is identical to the default value.

            var propertyNames = await defaultProperties.GetPropertyNamesAsync().ConfigureAwait(true);
            if (!propertyNames.Contains(PropertyName))
            {
                if (Version.TryParse(unevaluatedPropertyValue, out Version version))
                {
                    var defaultVersion = await GetDefaultVersionAsync(defaultProperties).ConfigureAwait(true);
                    if (version.Equals(defaultVersion))
                    {
                        return null;
                    }
                }
            }            

            return await base.OnSetPropertyValueAsync(unevaluatedPropertyValue, defaultProperties, dimensionalConditions).ConfigureAwait(true);
        }
    }
}
