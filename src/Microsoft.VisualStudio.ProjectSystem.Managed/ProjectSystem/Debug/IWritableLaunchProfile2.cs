// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    /// <summary>
    ///     Provides an implementation of <see cref="IWritableLaunchProfile"/> with an
    ///     additional property <see cref="EnableUnmanagedDebugging"/> for controlling
    ///     mixed mode debugging.
    /// </summary>
    public interface IWritableLaunchProfile2 : IWritableLaunchProfile   
    {
        bool EnableUnmanagedDebugging { get; set; }

        // Convert back to the immutable form
        ILaunchProfile2 ToLaunchProfile2();
    }
}
