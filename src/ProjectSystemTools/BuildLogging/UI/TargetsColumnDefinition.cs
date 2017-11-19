// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.Targets)]
    internal sealed class TargetsColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.Targets;

        public override string DisplayName => Resources.TargetsHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(TableKeyNames.Targets, out var value) && value != null && 
                value is IEnumerable<string> targets &&
                targets.Any())
            {
                content = targets.Aggregate((current, next) => $"{current};{next}");
                return true;
            }

            content = null;
            return false;
        }
    }
}
