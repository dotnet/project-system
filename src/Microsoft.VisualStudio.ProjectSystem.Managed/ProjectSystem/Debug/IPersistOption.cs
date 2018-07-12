// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends ILaunchProfile to support in-memory (not persisted) profiles
    /// </summary>
    public interface IPersistOption
    {
        bool DoNotPersist { get; }
    }
}
