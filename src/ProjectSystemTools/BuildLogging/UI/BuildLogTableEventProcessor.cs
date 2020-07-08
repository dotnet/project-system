// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class BuildLogTableEventProcessor : TableControlEventProcessorBase
    {
        private readonly BuildLoggingToolWindow _toolWindow;

        public BuildLogTableEventProcessor(BuildLoggingToolWindow toolWindow)
        {
            _toolWindow = toolWindow;
        }

        public override void PostprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e) => _toolWindow.OpenLog(entryHandle);

        public override void PreprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ProjectSystemToolsPackage.UpdateQueryStatus();

            base.PreprocessSelectionChanged(e);
        }
    }
}
