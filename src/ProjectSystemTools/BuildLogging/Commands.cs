//-------------------------------------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal static class Commands
    {
        public static RoutedUICommand Open { get; }

        static Commands()
        {
            Open = new RoutedUICommand(Resources.OpenCommand, "Open", typeof(Commands));
        }
    }
}