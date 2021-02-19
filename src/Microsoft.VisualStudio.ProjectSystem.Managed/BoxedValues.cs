// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Managed
{
    /// <summary>
    /// Contains boxed versions of well-known values to avoid allocating unnecessarily.
    /// </summary>
    internal static class BoxedValues
    {
        /// <summary>
        /// Returns an object containing <see langword="true"/>.
        /// </summary>
        public static readonly object True = true;

        /// <summary>
        /// Returns an object containing <see langword="false"/>.
        /// </summary>
        public static readonly object False = false;
    }
}
