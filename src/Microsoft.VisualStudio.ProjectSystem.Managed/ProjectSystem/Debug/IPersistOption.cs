// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends <see cref="ILaunchProfile"/> to support in-memory (not persisted) profiles.
    /// </summary>
    public interface IPersistOption
    {
        bool DoNotPersist { get; }
    }
}
