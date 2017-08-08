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
    [Name(TableColumnNames.Build)]
    internal sealed class BuildColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Build;

        public override string DisplayName => Resources.BuildHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(TableKeyNames.Operation, out var operationValue) && operationValue != null && operationValue is BuildOperation operation &&
                entry.TryGetValue(TableKeyNames.OperationTime, out var timeValue) && timeValue != null && timeValue is DateTime time)
            {
                content = $"{operation} ({time:s})";
                return true;
            }

            content = null;
            return false;
        }
    }
}
