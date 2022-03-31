// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents a set of ordered active configured objects, such as <see cref="ConfiguredProject"/> objects or <see cref="ProjectConfiguration"/>
    ///     objects, and the names of the configuration dimensions that participated in the calculation of the active configured objects.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the active configured objects, typically <see cref="ProjectConfiguration"/> or <see cref="ConfiguredProject"/>.
    /// </typeparam>
    internal class ActiveConfiguredObjects<T>
    {
        /// <summary>
        ///     Initializes a new instance of <see cref="ActiveConfiguredObjects{T}"/> with the specified objects and configurations
        ///     dimension names.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="ImmutableArray{T}"/> of the active configured objects.
        /// </param>
        /// <param name="dimensionNames">
        ///     An <see cref="IImmutableSet{T}"/> containing the names of the configuration dimensions that participated in
        ///     the calculation of the active configured objects, or empty if no dimensions participated in the calculation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="objects"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="dimensionNames"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="objects"/> is empty.
        /// </exception>
        public ActiveConfiguredObjects(ImmutableArray<T> objects, IImmutableSet<string> dimensionNames)
        {
            Requires.NotNull(dimensionNames, nameof(dimensionNames));
            Requires.Argument(!objects.IsDefaultOrEmpty, nameof(objects), "Must not be default or empty.");

            Objects = objects;
            DimensionNames = dimensionNames;
        }

        /// <summary>
        ///     Gets the active configured objects.
        /// </summary>
        /// <value>
        ///     An <see cref="ImmutableArray{T}"/> of the active configured objects.
        /// </value>
        /// <remarks>
        ///     The order in the returned <see cref="ImmutableArray{T}"/> matches the declared ordered within
        ///     the project file.
        /// </remarks>
        public ImmutableArray<T> Objects { get; }

        /// <summary>
        ///     Gets the names of the configuration dimensions that participated in the calculation of the active configured objects.
        /// </summary>
        /// <value>
        ///     An <see cref="IImmutableSet{T}"/> containing the names of the configuration dimensions that participated in the
        ///     calculation of the active configured objects, or empty if no dimensions participated in the calculation.
        /// </value>
        public IImmutableSet<string> DimensionNames { get; }
    }
}
