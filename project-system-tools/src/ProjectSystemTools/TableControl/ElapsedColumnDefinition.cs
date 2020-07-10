// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.Elapsed)]
    internal sealed class ElapsedColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Elapsed;

        public override string DisplayName => TableControlResources.ElapsedHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 60.0;

        public override GridLength ColumnDefinition => new GridLength(60);

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement content)
        {
            if (entry.TryGetValue(TableKeyNames.Status, out var statusValue) && statusValue != null && statusValue is BuildStatus status
                && status != BuildStatus.Running
                && entry.TryGetValue(TableKeyNames.Elapsed, out var value) && value != null && value is TimeSpan timeSpan)
            {
                content = new TextBlock
                {
                    Text = timeSpan.TotalSeconds.ToString("N3"),
                    TextAlignment = TextAlignment.Right
                };
                return true;
            }

            content = null;
            return false;
        }
    }
}
