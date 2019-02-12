// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
