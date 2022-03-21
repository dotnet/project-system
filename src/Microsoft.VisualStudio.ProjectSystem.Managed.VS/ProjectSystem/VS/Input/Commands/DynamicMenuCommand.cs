// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Implementation of an OleMenuCommand which supports the DynamicStart (like an MRU list) type
    /// of commands
    /// </summary>
    internal abstract class DynamicMenuCommand : OleMenuCommand
    {
        public int MaxCount { get; protected set; }

        protected DynamicMenuCommand(CommandID id, int maxCount)
            : base(ExecHandler, delegate
            { }, QueryStatusHandler, id)
        {
            MaxCount = maxCount;
        }

        /// <summary>
        /// Derived classes need to implement the following two methods. They must return false
        /// if the command is not handled (index is out of range)
        /// </summary>
        public abstract bool ExecCommand(int cmdIndex, EventArgs e);

        public abstract bool QueryStatusCommand(int cmdIndex, EventArgs e);

        /// <summary>
        /// Overridden to set the MatchedCmdId. This is used later by QS\Exec when determining the
        /// index of the current selection.
        /// </summary>
        public override bool DynamicItemMatch(int cmdId)
        {
            int index = cmdId - CommandID.ID;

            // Is the index in range?
            if (index >= 0 && index < MaxCount)
            {
                MatchedCommandId = cmdId;
                return true;
            }

            // No match, clear command id and return false
            MatchedCommandId = 0;
            return false;
        }

        /// <summary>
        /// Returns the current index (0 based) of the command that is currently selected (set by
        /// MatchedCommandId).
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                // Index is the current matched ID minus the base
                if (MatchedCommandId > 0)
                {
                    return MatchedCommandId - CommandID.ID;
                }
                return 0;
            }
        }

        /// <summary>
        /// Exec handler called when one of the menu items is selected. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        protected static void ExecHandler(object sender, EventArgs e)
        {
            if (sender is not DynamicMenuCommand command)
            {
                return;
            }

            int cmdIndex = command.CurrentIndex;
            if (cmdIndex >= 0 && cmdIndex < command.MaxCount)
            {
                // Only return if command was handled
                if (command.ExecCommand(cmdIndex, e))
                {
                    return;
                }
            }

            // We want to make sure to clear the matched commandid.
            command.MatchedCommandId = 0;
        }

        /// <summary>
        /// QueryStatus handler called to update the status of the menu items. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        protected static void QueryStatusHandler(object sender, EventArgs e)
        {
            if (sender is not DynamicMenuCommand command)
            {
                return;
            }

            int cmdIndex = command.CurrentIndex;
            if (cmdIndex >= 0 && cmdIndex < command.MaxCount)
            {
                // Only return if command was handled
                if (command.QueryStatusCommand(cmdIndex, e))
                {
                    // We want to make sure to clear the matched commandid.
                    command.MatchedCommandId = 0;
                    return;
                }
            }
            // If we get this far, hide the command
            command.Visible = false;
            command.Enabled = false;
            command.MatchedCommandId = 0;
        }
    }
}
