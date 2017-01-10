// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Controls;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public static class UIProfilePropertyName
    {
         public const string Executable = "Executable";
         public const string Arguments = "Arguments";
         public const string LaunchUrl = "LaunchUrl";
         public const string EnvironmentVariables = "EnvironmentVariables";
         public const string WorkingDirectory = "WorkingDirectory";
    }

    /// <summary>
    /// Interface definition which allows a launch settings provider to participate in the debug property page UI. The Set of 
    /// LaunchSettingUIProviders provides the set of entries for the debug dropdown command list.
    /// </summary>
    public interface ILaunchSettingsUIProvider
    {
        /// <summary>
        /// The name of the command that is written to the launchSettings.json file
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// The name to display in the dropdown for this command
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Current supports these property names which map to the the default set of properties
        ///     "Executable"
        ///     "Arguments"
        ///     "LaunchUrl"
        ///     "EnvironmentVariables"
        ///     "WorkingDirectory"
        /// The names are case insensitive 
        /// </summary>
        bool ShouldEnableProperty(string propertyName);

        /// <summary>
        /// The UI to be displayed. This control is placed below all the common controls on the dialog. It is OK to return
        /// null if there are no custom controls but still want to add a new command to the list. Example is the Executable and Project
        /// commands. Neither provide custom controls (though arguable executable should).
        /// </summary>
        UserControl CustomUI { get; }

        /// <summary>
        /// Called when the selected profile changes to a profile which matches this command. curSettings will contain 
        /// the current values from the page, and activeProfile will point to the active one.
        /// </summary>
        void ProfileSelected(IWritableLaunchSettings curSettings);
    }
}
