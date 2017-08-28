// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    /// <summary>
    /// Manages the state of the Project File editor, coordinating the components that display the editor, watch for
    /// project updates, update the project when the editor is saved, and manages correct disposal of these components.
    /// </summary>
    internal interface IProjectFileEditorPresenter
    {
        /// <summary>
        /// Opens the project file editor. Switches to the existing editor if one is already open.
        /// </summary>
        Task OpenEditorAsync();

        /// <summary>
        /// Checks to see whether or not the window can currently be closed, saving if necessary.
        /// </summary>
        /// <returns>True if the window can be closed, false if it cannot</returns>
        Task<bool> CanCloseWindowAsync();

        /// <summary>
        /// Closes the project file editor.
        /// </summary>
        /// <returns></returns>
        Task CloseCurrentEditorAsync();

        /// <summary>
        /// Schedules a task to update the open project file editor with the latest project file.
        /// </summary>
        JoinableTask ScheduleProjectFileUpdate();

        /// <summary>
        /// Saves the current contents of the editor to the project.
        /// </summary>
        Task SaveProjectFileAsync();
    }
}
