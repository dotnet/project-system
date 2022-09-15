// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static class WpfHelper
    {
        public static DataGridCell? GetCell(DataGrid dataGrid, int row, int column)
        {
            DataGridRow rowContainer = GetRow(dataGrid, row);

            if (rowContainer is not null)
            {
                DataGridCellsPresenter? presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                if (presenter is not null)
                {
                    // try to get the cell but it may possibly be virtualized
                    var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                    if (cell is null)
                    {
                        // now try to bring into view and retrieve the cell
                        dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                        cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                    }

                    return cell;
                }
            }
            return null;
        }

        public static DataGridRow GetRow(DataGrid dataGrid, int index)
        {
            var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row is null)
            {
                // may be virtualized, bring into view and try again
                dataGrid.ScrollIntoView(dataGrid.Items[index]);
                row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static T? GetVisualChild<T>(Visual parent) where T : Visual
        {
            T? child = null;
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child is null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child is not null)
                {
                    break;
                }
            }
            return child;
        }
    }
}
