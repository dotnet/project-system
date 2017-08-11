// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    [Export(typeof(ITableColumnDefinition))]
    [Name(TableColumnNames.DesignTime)]
    internal sealed class DesignTimeColumnDefinition : TableColumnDefinitionBase
    {
        public override string Name => TableColumnNames.DesignTime;

        public override string DisplayName => Resources.DesignTimeHeaderLabel;

        public override StringComparer Comparer => StringComparer.Ordinal;

        public override double MinWidth => 100.0;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(TableKeyNames.DesignTime, out var value) && value != null && value is bool isDesignTime)
            {
                content = isDesignTime.ToString();
                return true;
            }

            content = null;
            return false;
        }
    }
}
