// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class ProjectPropertiesExtensions
    {
        /// <summary>
        /// Saves the unevaluated value of <paramref name="propertyName"/>, if any, to <paramref name="storage"/>.
        /// </summary>
        public static async Task SaveValueIfCurrentlySetAsync(this IProjectProperties properties, string propertyName, ITemporaryPropertyStorage storage)
        {
            string? currentPropertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);
            if (!Strings.IsNullOrEmpty(currentPropertyValue))
            {
                storage.AddOrUpdatePropertyValue(propertyName, currentPropertyValue);
            }
        }

        /// <summary>
        /// If <paramref name="propertyName"/> is not currently set restores the saved value, if any, from <paramref name="storage"/>.
        /// </summary>
        public static async Task RestoreValueIfNotCurrentlySetAsync(this IProjectProperties properties, string propertyName, ITemporaryPropertyStorage storage)
        {
            string? currentPropertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);
            if (string.IsNullOrEmpty(currentPropertyValue)
                && storage.GetPropertyValue(propertyName) is string previousValue)
            {
                await properties.SetPropertyValueAsync(propertyName, previousValue);
            }
        }
    }
}
