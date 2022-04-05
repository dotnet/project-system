// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Provides <see langword="static"/> extensions for <see cref="IComparable"/> instances.
    /// </summary>
    internal static class ComparableExtensions
    {
        /// <summary>
        ///     Returns a value indicating whether the current instance is later than the
        ///     specified <see cref="IComparable"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="comparable"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsLaterThan(this IComparable source, IComparable comparable)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(comparable, nameof(comparable));

            return source.CompareTo(comparable) > 0;
        }

        /// <summary>
        ///     Returns a value indicating whether the current instance is later than or
        ///     equal to the specified <see cref="IComparable"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="comparable"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsLaterThanOrEqualTo(this IComparable source, IComparable comparable)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(comparable, nameof(comparable));

            return source.CompareTo(comparable) >= 0;
        }

        /// <summary>
        ///     Returns a value indicating whether the current instance is earlier than the
        ///     specified <see cref="IComparable"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="comparable"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsEarlierThan(this IComparable source, IComparable comparable)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(comparable, nameof(comparable));

            return source.CompareTo(comparable) < 0;
        }

        /// <summary>
        ///     Returns a value indicating whether the current instance is earlier than or equal
        ///     to the specified <see cref="IComparable"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="comparable"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsEarlierThanOrEqualTo(this IComparable source, IComparable comparable)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(comparable, nameof(comparable));

            return source.CompareTo(comparable) <= 0;
        }
    }
}
