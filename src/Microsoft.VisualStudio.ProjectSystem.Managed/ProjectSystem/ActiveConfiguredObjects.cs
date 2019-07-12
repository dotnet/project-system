// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        ///     An <see cref="IReadOnlyList{T}"/> of the active configured objects.
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
        public ActiveConfiguredObjects(IReadOnlyList<T> objects, IImmutableSet<string> dimensionNames)
        {
            Requires.NotNull(dimensionNames, nameof(dimensionNames));
            Requires.NotNull(objects, nameof(objects));

            if (objects.Count == 0)
                throw new ArgumentException(null, nameof(objects));

            Objects = objects;
            DimensionNames = dimensionNames;
        }

        /// <summary>
        ///     Gets the active configured objects.
        /// </summary>
        /// <value>
        ///     An <see cref="IReadOnlyList{T}"/> of the active configured objects.
        /// </value>
        /// <remarks>
        ///     The order in the returned <see cref="IReadOnlyList{T}"/> matches the declared ordered within
        ///     the project file.
        /// </remarks>
        public IReadOnlyList<T> Objects
        {
            get;
        }

        /// <summary>
        ///     Gets the names of the configuration dimensions that participated in the calculation of the active configured objects.
        /// </summary>
        /// <value>
        ///     An <see cref="IImmutableSet{T}"/> containing the names of the configuration dimensions that participated in the
        ///     calculation of the active configured objects, or empty if no dimensions participated in the calculation.
        /// </value>
        public IImmutableSet<string> DimensionNames
        {
            get;
        }
    }
}
