// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Input;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
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