// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an <see cref="UnconfiguredProject"/> access to exports from the active
    ///     <see cref="ConfiguredProject"/>. This is the plural version of <see cref="IActiveConfiguredValue{T}"/>.
    /// </summary>
    internal interface IActiveConfiguredValues<T>
        where T : class
    {
        /// <summary>
        ///     Gets the applicable values from the active <see cref="ConfiguredProject"/>.
        /// </summary>
        public IEnumerable<Lazy<T>> Values
        {
            get;
        }
    }
}
