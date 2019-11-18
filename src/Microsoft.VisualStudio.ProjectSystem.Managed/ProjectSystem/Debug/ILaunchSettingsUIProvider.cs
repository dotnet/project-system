// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Controls;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Allows a launch settings provider to modify and extend the debug property page.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface each contribute an entry to the drop-down list of
    /// launch profiles in the "Debug" project property page.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface ILaunchSettingsUIProvider
    {
        /// <summary>
        /// The value of the <c>commandName</c> property written to the <c>launchSettings.json</c> file.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// The user-friendly name of this this launch provider.
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Allows a launch provider to suppress default properties from the UI.
        /// </summary>
        /// <remarks>
        /// Currently supports the following default properties:
        /// <list type="bullet">
        ///   <item><c>Executable</c></item>
        ///   <item><c>Arguments</c></item>
        ///   <item><c>LaunchUrl</c></item>
        ///   <item><c>EnvironmentVariables</c></item>
        ///   <item><c>WorkingDirectory</c></item>
        /// </list>
        /// Names should be treated case-insensitively. Constants for these names exist in <see cref="UIProfilePropertyName"/>.
        /// </remarks>
        bool ShouldEnableProperty(string propertyName);

        /// <summary>
        /// Provides an optional UI control for this launch provider to be displayed below other controls on the dialog.
        /// May be <see langword="null"/> if the launch provider does not have any dedicated UI.
        /// </summary>
        UserControl? CustomUI { get; }

        /// <summary>
        /// Called when the selected profile changes to a profile which matches this command.
        /// </summary>
        /// <param name="curSettings">The page's current values.</param>
        void ProfileSelected(IWritableLaunchSettings curSettings);
    }
}
