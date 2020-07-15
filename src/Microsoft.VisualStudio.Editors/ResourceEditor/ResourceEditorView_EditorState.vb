' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Collections.Specialized

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Partial Friend Class ResourceEditorView

        ''' <summary>
        ''' A class that contains the current editor state (current category, grid column widths, etc.).
        '''   This is useful for when the resx file is changed by another editor and we have to reload.
        '''   We can use this saved state to put the editor into close to the original state after
        '''   the reload (which completely disposes the old designers and creates new ones).
        ''' </summary>
        ''' <remarks>
        ''' EditorState is a private class of ResourceEditorView.  This gives it access to all private
        '''   members of ResourceEditorView, but we can still keep it in a separate file through
        '''   the use of partial classes.
        ''' </remarks>
        Friend NotInheritable Class EditorState
            Private _statePersisted As Boolean

            'Current category name.  We save by name instead of reference in case the category no longer exists, etc.
            Private _currentCategoryName As String

            'Names of the currently selected resources (in the listview or stringtable, whichever is showing).
            Private _selectedResourceNames() As String

            'Widths of the columns in the string table (whether or not the stringtable is currently showing)
            Private _stringTableColumnWidths() As Integer

            'Current listview view (thumbnail, icons, etc.) for each category, hashed by category name (whether or not these categories are showing)
            Private ReadOnly _resourceViewHash As New ListDictionary

            'Current sorter for each category, hashed by category name (whether or not these categories are showing)
            Private ReadOnly _categorySorter As New ListDictionary

            'Widths of the columns in the listview's details view (whether or not the listview is currently showing)
            Private _listViewColumnWidths() As Integer

            ''' <summary>
            ''' Public constructor.  Sets it to an empty state.
            ''' </summary>
            Public Sub New()
                _statePersisted = False
            End Sub

            ''' <summary>
            ''' Returns whether or not state has actually been persisted into this object.
            ''' </summary>
            Public ReadOnly Property StatePersisted As Boolean
                Get
                    Return _statePersisted
                End Get
            End Property

            ''' <summary>
            ''' Clears all the editor state into a non-persisted state.
            ''' </summary>
            Private Sub Clear()
                _statePersisted = False
                _currentCategoryName = Nothing
                _selectedResourceNames = Nothing
                _stringTableColumnWidths = Nothing
                _resourceViewHash.Clear()
                _categorySorter.Clear()
                _listViewColumnWidths = Nothing
            End Sub

            ''' <summary>
            ''' Persists state from a given resource editor view into this object.
            ''' </summary>
            ''' <param name="View">Resource editor view object to save state from.</param>
            Public Sub PersistStateFrom(View As ResourceEditorView)
                Debug.Assert(View IsNot Nothing, "View can't be Nothing in EditorState")

                Try
                    Clear()

                    'Current category
                    If View._currentCategory IsNot Nothing Then
                        _currentCategoryName = View._currentCategory.ProgrammaticName
                    End If

                    'Selected resources (don't include selected cells, just actual selected rows in the string table)
                    Dim SelectedResources() As Resource = View.GetSelectedResources()
                    If SelectedResources IsNot Nothing AndAlso SelectedResources.Length > 0 Then
                        'For the string table, 
                        ReDim _selectedResourceNames(SelectedResources.Length - 1)
                        For i As Integer = 0 To SelectedResources.Length - 1
                            _selectedResourceNames(i) = SelectedResources(i).Name
                        Next
                    End If

                    'String table column widths
                    ReDim _stringTableColumnWidths(View.StringTable.ColumnCount - 1)
                    For i As Integer = 0 To View.StringTable.ColumnCount - 1
                        _stringTableColumnWidths(i) = View.StringTable.Columns(i).Width
                    Next

                    'ListView column widths
                    ReDim _listViewColumnWidths(View._resourceListView.Columns.Count - 1)
                    For i As Integer = 0 To View._resourceListView.Columns.Count - 1
                        _listViewColumnWidths(i) = View._resourceListView.Columns(i).Width
                    Next

                    'ResourceView mode for all categories
                    For Each Category As Category In View._categories
                        _resourceViewHash.Add(Category.ProgrammaticName, Category.ResourceView)
                        _categorySorter.Add(Category.ProgrammaticName, Category.Sorter)
                    Next

                    _statePersisted = True
                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Exception depersisting editor state - state will not be restored", $"{NameOf(ResourceEditorView)}-{NameOf(EditorState)}")
                    Clear()

                    'Exception can be safely ignored.  They just won't get the state restored later.
                    Exit Sub
                End Try
            End Sub

            ''' <summary>
            ''' Sets up the given resource editor view instance with the state which has previously been saved in
            '''   this object.
            ''' </summary>
            ''' <param name="View">The resource editor view instance to depersist the old state into</param>
            Public Sub DepersistStateInto(View As ResourceEditorView)
                If _statePersisted Then
                    Try
                        Debug.Assert(View IsNot Nothing, "View can't be Nothing in EditorState")

                        'String table column widths
                        If _stringTableColumnWidths IsNot Nothing AndAlso _stringTableColumnWidths.Length = View.StringTable.ColumnCount Then
                            For i As Integer = 0 To _stringTableColumnWidths.Length - 1
                                Try
                                    View.StringTable.Columns(i).Width = _stringTableColumnWidths(i)
                                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Unable to set stringtable column width in restoring editor state", $"{NameOf(ResourceEditorView)}-{NameOf(EditorState)}")
                                    'Ignore exceptions if unable to set a column width (columns can have minimum widths)
                                End Try
                            Next
                        End If

                        'ListView column widths
                        If _listViewColumnWidths IsNot Nothing AndAlso _listViewColumnWidths.Length = View._resourceListView.Columns.Count Then
                            For i As Integer = 0 To _listViewColumnWidths.Length - 1
                                Try
                                    View._resourceListView.Columns(i).Width = _listViewColumnWidths(i)
                                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Unable to set listview column width in restoring editor state", $"{NameOf(ResourceEditorView)}-{NameOf(EditorState)}")
                                    'Ignore exceptions
                                End Try
                            Next
                        End If

                        'Current category
                        Dim CurrentCategory As Category = View._categories(_currentCategoryName)
                        If CurrentCategory IsNot Nothing Then
                            View.SwitchToCategory(CurrentCategory)
                        End If

                        'ResourceView mode for all categories
                        For Each Entry As DictionaryEntry In _resourceViewHash
                            Dim Category As Category = View._categories(CStr(Entry.Key))
                            If Category IsNot Nothing Then
                                View.ChangeResourceViewForCategory(Category, DirectCast(Entry.Value, ResourceListView.ResourceView))
                            End If
                        Next

                        For Each Entry As DictionaryEntry In _categorySorter
                            Dim Category As Category = View._categories(CStr(Entry.Key))
                            If Category IsNot Nothing Then
                                View.ChangeSorterForCategory(Category, DirectCast(Entry.Value, IComparer(Of Resource)))
                            End If
                        Next

                        'Selected resources
                        If _selectedResourceNames IsNot Nothing Then
                            'We have to search for the resources by name.  Some of them may no longer exist.
                            '  We'll select the ones we can still find.
                            Dim ResourcesToSelect As New List(Of Resource)
                            For Each Name As String In _selectedResourceNames
                                Dim Resource As Resource = View._resourceFile.FindResource(Name)
                                If Resource IsNot Nothing Then
                                    ResourcesToSelect.Add(Resource)
                                End If
                            Next
                            View.HighlightResources(ResourcesToSelect, SelectInPropertyGrid:=True)
                        End If

                    Catch ex As Exception When Common.ReportWithoutCrash(ex, "Exception saving editor state - state will not be saved", $"{NameOf(ResourceEditorView)}-{NameOf(EditorState)}")
                        Clear()

                        'Exception can be safely ignored.  They just won't get the state restored later.
                        Exit Sub
                    End Try
                End If
            End Sub
        End Class

    End Class

End Namespace

