﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal class TableSettingLoader
    {
        private const string ColumnWidth = "Width";
        private const string ColumnVisibility = "Visibility";
        private const string ColumnSortPriority = "SortPriority";
        private const string ColumnSortDown = "DescendingSort";
        private const string ColumnOrder = "ColumnOrder";
        private const string ColumnGroupingPriority = "GroupingPriority";

        private static string CreateColumnsKey(string window) => $"ProjectSystemTools\\{window}\\Columns";

        private static string CreateSettingsKey(string window) => $"ProjectSystemTools\\{window}\\Settings";

        public static IEnumerable<ColumnState> LoadSettings(string window, IEnumerable<ColumnState> defaultColumns)
        {
            var columns = new List<Tuple<int, ColumnState>>();

            using (RegistryKey rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.ServiceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: false))
            {
                using RegistryKey columnsSubKey = rootKey.OpenSubKey(CreateColumnsKey(window), writable: false);
                if (columnsSubKey == null)
                {
                    return defaultColumns;
                }

                foreach (string columnName in columnsSubKey.GetSubKeyNames())
                {
                    using RegistryKey columnSubKey = columnsSubKey.OpenSubKey(columnName, writable: false);
                    if (columnSubKey == null)
                    {
                        continue;
                    }

                    bool descendingSort = (int)columnSubKey.GetValue(ColumnSortDown, 1) != 0;
                    int sortPriority = (int)columnSubKey.GetValue(ColumnSortPriority, 0);

                    int groupingPriority = (int)columnSubKey.GetValue(ColumnGroupingPriority, 0);

                    int columnOrder = (int)columnSubKey.GetValue(ColumnOrder, int.MaxValue);
                    bool visibility = (int)columnSubKey.GetValue(ColumnVisibility, 0) != 0;
                    int width = (int)columnSubKey.GetValue(ColumnWidth, 20);

                    var state = new ColumnState2(columnName, visibility, width, sortPriority, descendingSort, groupingPriority);

                    columns.Add(new Tuple<int, ColumnState>(columnOrder, state));
                }
            }

            columns.Sort((a, b) => a.Item1 - b.Item1);

            return columns.Select(a => a.Item2);
        }

        public static bool SaveSettings(string window, IWpfTableControl control)
        {
            IReadOnlyList<ColumnState> columns = control.ColumnStates;

            using (RegistryKey rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.ServiceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true))
            {
                using RegistryKey columnsSubKey = rootKey.CreateSubKey(CreateColumnsKey(window));
                if (columnsSubKey == null)
                {
                    return false;
                }

                for (int i = 0; i < columns.Count; i++)
                {
                    ColumnState column = columns[i];

                    using RegistryKey columnSubKey = columnsSubKey.CreateSubKey(column.Name);
                    if (columnSubKey == null)
                    {
                        continue;
                    }

                    columnSubKey.SetValue(ColumnOrder, i, RegistryValueKind.DWord);
                    columnSubKey.SetValue(ColumnVisibility, column.IsVisible ? 1 : 0, RegistryValueKind.DWord);
                    columnSubKey.SetValue(ColumnWidth, (int)column.Width, RegistryValueKind.DWord);

                    columnSubKey.SetValue(ColumnSortDown, column.DescendingSort ? 1 : 0, RegistryValueKind.DWord);
                    columnSubKey.SetValue(ColumnSortPriority, column.SortPriority, RegistryValueKind.DWord);

                    if (column is ColumnState2 cs2)
                    {
                        columnSubKey.SetValue(ColumnGroupingPriority, cs2.GroupingPriority, RegistryValueKind.DWord);
                    }
                }
            }

            return true;
        }

        public static void LoadSwitch(string window, string name, bool defaultValue, out bool value)
        {
            using RegistryKey rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.ServiceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: false);
            using RegistryKey settingsSubKey = rootKey.OpenSubKey(CreateSettingsKey(window), writable: false);
            value = settingsSubKey == null ? defaultValue : (int)settingsSubKey.GetValue(name, 0) != 0;
        }

        public static void SaveSwitch(string window, string name, bool value)
        {
            using RegistryKey rootKey = VSRegistry.RegistryRoot(ProjectSystemToolsPackage.ServiceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true);
            using RegistryKey settingsSubKey = rootKey.CreateSubKey(CreateSettingsKey(window));
            settingsSubKey?.SetValue(name, value ? 1 : 0);
        }
    }
}
