// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a modifiable launch settings
    /// </summary>
    public interface IWritableLaunchSettings
    {
        IWritableLaunchProfile? ActiveProfile { get; set; }

        List<IWritableLaunchProfile> Profiles { get; }

        Dictionary<string, object> GlobalSettings { get; }

        /// <summary>
        /// Converts back to the immutable form.
        /// </summary>
        ILaunchSettings ToLaunchSettings();
    }
}
