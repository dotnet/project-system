// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.ViewModel.BuildLoggingToolWindow;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    public partial class BuildLoggingToolWindow : UserControl
    {
        public BuildLoggingToolWindow()
        {
            InitializeComponent();
            DataContext = new ToolWindow();
        }
    }
}
