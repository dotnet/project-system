// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends IWritableLaunchProfile to handle in-memory only profiles
    /// </summary>
    public interface IWritablePersistOption : IPersistOption
    {
        new bool DoNotPersist { get; set; }
    }
}
