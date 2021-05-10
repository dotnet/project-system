// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Specifies the "backing store" for an <see cref="ILaunchProfileExtensionValueProvider"/>
    /// or <see cref="IGlobalSettingExtensionValueProvider"/>. This determines where the
    /// property value is read from/stored to.
    /// </summary>
    public enum ExportLaunchProfileExtensionValueProviderScope
    {
        /// <summary>
        /// Extension properties are backed by an <see cref="ILaunchProfile"/>. The related
        /// type must implement <see cref="ILaunchProfileExtensionValueProvider"/>.
        /// </summary>
        LaunchProfile,
        /// <summary>
        /// Extensions properties are backed by <see cref="ILaunchSettings.GlobalSettings"/>.
        /// The related type must implement <see cref="IGlobalSettingExtensionValueProvider"/>.
        /// </summary>
        GlobalSettings
    }
}
