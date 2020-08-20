// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs (this wraps this API)

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an <see cref="UnconfiguredProject"/> access to an export from the active
    ///     <see cref="ConfiguredProject"/>. This is the singular version of <see cref="IActiveConfiguredValues{T}"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This replaces <see cref="ActiveConfiguredProject{T}"/> by returning <see langword="null"/>
    ///         instead of throwing when the value does not exist.
    ///     </para>
    ///     <para>
    ///         Consumers should specify a nullable type for <typeparamref name="T"/> if that import will be
    ///         satisfied by an export that will be applied to a particular capability.
    ///     </para>
    /// </remarks>
    internal interface IActiveConfiguredValue<T>
        where T : class?
    {
        /// <summary>
        ///     Gets the value from the active <see cref="ConfiguredProject"/>; otherwise,
        ///     <see langword="null"/> if it does not exist.
        /// </summary>
        public T Value
        {
            get;
        }
    }
}
