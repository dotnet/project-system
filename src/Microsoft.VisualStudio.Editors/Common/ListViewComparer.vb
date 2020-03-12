' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.Common

    ''' ;ListViewComparer
    ''' <summary>
    ''' IComparer for ListView. 
    ''' - Sort the ListView based on the current column or the first column if current column values are equal.
    ''' - Shared method to handle a column click event and sort the list view.
    ''' </summary>
    Friend Class ListViewComparer
        Implements IComparer

        ' which column is used to sort the list view
        Private _sortColumn As Integer

        Private _sorting As SortOrder = SortOrder.Ascending

        ''' <summary>
        '''  Which column should be used to sort the list. Start from 0
        ''' </summary>
        Public Property SortColumn As Integer
            Get
                Return _sortColumn
            End Get
            Set
                _sortColumn = value
            End Set
        End Property

        ''' <summary>
        '''  which order, Ascending or Descending
        ''' </summary>
        Public Property Sorting As SortOrder
            Get
                Return _sorting
            End Get
            Set
                _sorting = value
            End Set
        End Property

        ''' <summary>
        '''  Compare two list items
        ''' </summary>
        Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
            Dim ret As Integer = String.Compare(GetColumnValue(x, _sortColumn), GetColumnValue(y, _sortColumn), StringComparison.OrdinalIgnoreCase)
            If ret = 0 AndAlso _sortColumn <> 0 Then
                ret = String.Compare(GetColumnValue(x, 0), GetColumnValue(y, 0), StringComparison.OrdinalIgnoreCase)
            End If
            If _sorting = SortOrder.Descending Then
                ret = -ret
            End If
            Return ret
        End Function

        ''' <summary>
        '''  Get String Value of one column
        ''' </summary>
        Private Shared Function GetColumnValue(obj As Object, column As Integer) As String
            Dim listItem = TryCast(obj, ListViewItem)
            If listItem IsNot Nothing Then
                Return listItem.SubItems.Item(column).Text
            End If

            Debug.Fail("RefComparer: obj was not an ListViewItem")
            Return String.Empty
        End Function

        Public Shared Sub HandleColumnClick(listView As ListView, comparer As ListViewComparer,
                e As ColumnClickEventArgs)
            Dim focusedItem As ListViewItem = listView.FocusedItem

            If e.Column <> comparer.SortColumn Then
                comparer.SortColumn = e.Column
                listView.Sorting = SortOrder.Ascending
            Else
                If listView.Sorting = SortOrder.Ascending Then
                    listView.Sorting = SortOrder.Descending
                Else
                    listView.Sorting = SortOrder.Ascending
                End If
            End If
            comparer.Sorting = listView.Sorting
            listView.Sort()

            If focusedItem IsNot Nothing Then
                listView.FocusedItem = focusedItem
            ElseIf listView.SelectedItems.Count > 0 Then
                listView.FocusedItem = listView.SelectedItems(0)
            End If
            If listView.FocusedItem IsNot Nothing Then
                listView.EnsureVisible(listView.FocusedItem.Index)
            End If
        End Sub
    End Class
End Namespace
