// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Provides extension methods for <see cref="IProperty"/> instances.
    /// </summary>
    internal static class PropertyExtensions
    {
        /// <summary>
        ///     Returns the value of the specific <see cref="IProperty"/> as <see cref="Guid"/>
        ///     or <see cref="Guid.Empty"/> if the value cannot be parsed.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<Guid> GetValueAsGuidAsync(this IProperty property)
        {
            Requires.NotNull(property, nameof(property));

            string? value = (string?)await property.GetValueAsync();

            if (Guid.TryParse(value, out Guid result))
            {
                return result;
            }

            return Guid.Empty;
        }
    }
}
