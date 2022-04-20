// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends the default profile with properties for environment variables and settings
    /// that preserve the order of these collections.
    /// </summary>
    public interface ILaunchProfile2 : ILaunchProfile
    {
        new ImmutableArray<(string Key, string Value)> EnvironmentVariables { get; }
        new ImmutableArray<(string Key, object Value)> OtherSettings { get; }
    }
}
