// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.Build
{
    internal static class BuildExtensions
    {
        /// <summary>
        ///     Gets the unescaped, unevaluated value.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="element"/> is <see langword="null" />.
        /// </exception>
        public static string GetUnescapedValue(this ProjectPropertyElement element)
        {
            Requires.NotNull(element, nameof(element));

            return ProjectCollection.Unescape(element.Value);
        }
    }
}
