// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.Elapsed)]
    internal sealed class ElapsedColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Elapsed;

        public override string DisplayName => Resources.ElapsedHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(TableKeyNames.Status, out var statusValue) && statusValue != null && statusValue is BuildStatus status &&
                status != BuildStatus.Running &&
                entry.TryGetValue(TableKeyNames.Elapsed, out var value) && value != null && value is TimeSpan timeSpan)
            {
                content = timeSpan.ToString("g");
                return true;
            }

            content = null;
            return false;
        }
    }
}
