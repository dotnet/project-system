// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

[ExportInterceptingPropertyValueProvider("TrimMode", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal class TrimModeValueProvider : InterceptingPropertyValueProviderBase
{
    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        // When setting the TrimMode, we also need to set the PublishTrimmed property to true.
        if (string.Equals(unevaluatedPropertyValue, "full", StringComparison.OrdinalIgnoreCase))
        {
            await defaultProperties.SetPropertyValueAsync("PublishTrimmed", "true");
        }
        else
        {
            // If the user sets the TrimMode to anything other than full, set the PublishTrimmed property to false.
            await defaultProperties.SetPropertyValueAsync("PublishTrimmed", "false");
        }

        return await base.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
    }
}
