// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
