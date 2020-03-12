// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text;
using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    ///     Provides extension methods for <see cref="StringBuilder"/> instances.
    /// </summary>
    internal static class StringBuilderExtensions
    {
        /// <summary>
        ///     Appends the text representation of the specified format to the
        ///     specified string builder.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///     The format specification in <paramref name="format"/> is invalid.
        /// </exception>
        public static void AppendFormat(this StringBuilder builder, in StringFormat format)
        {
            Requires.NotNull(builder, nameof(builder));

            int length = format.Length;

            if (length == 0)
            {
                builder.Append(format.Format);
            }
            else if (length == 1)
            {
                builder.AppendFormat(format.Format, format.Argument1);
            }
            else if (length == 2)
            {
                builder.AppendFormat(format.Format, format.Argument1, format.Argument2);
            }
            else if (length == 3)
            {
                builder.AppendFormat(format.Format, format.Argument1, format.Argument2, format.Argument3);
            }
            else
            {
                builder.AppendFormat(format.Format, format.Arguments);
            }
        }

        public static StringBuilder TrimEnd(this PooledStringBuilder builder, params char[] trimChars)
        {
            while (builder.Length > 0)
            {
                bool match = false;
                foreach (char c in trimChars)
                {
                    if (builder[builder.Length - 1] == c)
                    {
                        match = true;
                        builder.Length--;
                        break;
                    }
                }

                if (!match)
                {
                    return builder;
                }
            }

            return builder;
        }
    }
}
