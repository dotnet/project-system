// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class ProjectPropertiesExtensions
    {
        /// <summary>
        /// Saves the unevaluated value of <paramref name="propertyName"/>, if defined in context, to <paramref name="storage"/>.
        /// </summary>
        public static async Task SaveValueIfCurrentlySetAsync(this IProjectProperties properties, string propertyName, ITemporaryPropertyStorage storage)
        {
            if (!await properties.IsValueInheritedAsync(propertyName))
            {
                string? currentPropertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);
                if (!Strings.IsNullOrEmpty(currentPropertyValue))
                {
                    storage.AddOrUpdatePropertyValue(propertyName, currentPropertyValue);
                }
            }
        }

        /// <summary>
        /// Restores the saved value of <paramref name="propertyName"/>, if not defined in context, from <paramref name="storage"/>.
        /// </summary>
        public static async Task RestoreValueIfNotCurrentlySetAsync(this IProjectProperties properties, string propertyName, ITemporaryPropertyStorage storage, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (storage.GetPropertyValue(propertyName) is string previousValue)
            {
                bool inherited = await properties.IsValueInheritedAsync(propertyName);
                string? currentPropertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);

                if (inherited || string.IsNullOrEmpty(currentPropertyValue))
                {
                    await properties.SetPropertyValueAsync(propertyName, previousValue, dimensionalConditions);
                }
            }
        }
    }
}
