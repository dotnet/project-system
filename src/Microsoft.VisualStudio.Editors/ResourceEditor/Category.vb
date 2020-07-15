' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel.Design
Imports System.Globalization

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Represents a category of resources (e.g., "Strings", "Audio", "Images").
    '''   These categories are displayed in buttons at the top of the editor, and
    '''   allow the user to filter the currently-displayed resources by 
    '''   a single category at a time.
    ''' </summary>
    Friend NotInheritable Class Category

        ''' <summary>
        ''' Fired when the value of the ResourcesExist property changes
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Public Event ResourcesExistChanged(sender As Object, e As EventArgs)

        ''' <summary>
        ''' Indicates whether this category displays its resources in a stringtable
        '''   or a listview.
        ''' </summary>
        Public Enum Display
            StringTable 'Resources in this category displayed in a string table
            ListView    'Resources in this category displayed in a listview
        End Enum

        'Backing for public properties
        Private ReadOnly _associatedResourceTypeEditors() As ResourceTypeEditor
        Private ReadOnly _categoryDisplay As Display
        Private ReadOnly _localizedName As String
        Private ReadOnly _programmaticName As String
        Private _resourceCount As Integer
        Private _resourceView As ResourceListView.ResourceView = ResourceListView.ResourceView.Thumbnail
        Private _allowNewEntriesInStringTable As Boolean 'applies only to Display.StringTable
        Private _showTypeColumnInStringTable As Boolean 'applies only to Display.StringTable
        Private ReadOnly _menuCommand As MenuCommand
        Private _addCommand As EventHandler
        Private _sorter As IComparer(Of Resource)           ' how to sort resources in the category...

        '======================================================================
        '= Constructors =                                                     =
        '======================================================================

        ''' <summary>
        ''' Constructor for Category
        ''' </summary>
        ''' <param name="ProgrammaticName">A programmatic (non-localized) name for the category.  This name is never shown to the user, but is used for looking up a category by key.</param>
        ''' <param name="LocalizedName">A localized name for the category.  This name is the one shown to the user.</param>
        ''' <param name="CategoryDisplay">Whether this category uses a stringtable or listview.</param>
        ''' <param name="AssociatedResourceTypeEditors">A list of all resource type editors which are shown in this category (a resource type editor can only be shown in a single category).</param>
        Public Sub New(ProgrammaticName As String, LocalizedName As String, CategoryDisplay As Display, MenuCommand As MenuCommand, addCommand As EventHandler, ParamArray AssociatedResourceTypeEditors() As ResourceTypeEditor)
            Debug.Assert(ProgrammaticName <> "", "programmatic name must be non-empty")
            Debug.Assert(LocalizedName <> "", "localized name should be non-empty")
            Debug.Assert(AssociatedResourceTypeEditors IsNot Nothing, "Must be at least one resource type editor per category.")
            Debug.Assert(MenuCommand IsNot Nothing, "You must supply a MenuCommand")

            _programmaticName = ProgrammaticName
            _localizedName = LocalizedName
            _categoryDisplay = CategoryDisplay
            _showTypeColumnInStringTable = False
            _menuCommand = MenuCommand
            _addCommand = addCommand

            If AssociatedResourceTypeEditors IsNot Nothing Then
                _associatedResourceTypeEditors = AssociatedResourceTypeEditors
            Else
                _associatedResourceTypeEditors = Array.Empty(Of ResourceTypeEditor)
            End If
        End Sub

        '======================================================================
        '= Properties =                                                       =
        '======================================================================

        ''' <summary>
        ''' All resource type editors that are displayed in this category.  A particular
        '''   resource type editor may only be displayed in a single category.
        ''' </summary>
        Public ReadOnly Property AssociatedResourceTypeEditors As ResourceTypeEditor()
            Get
                Return _associatedResourceTypeEditors
            End Get
        End Property

        ''' <summary>
        ''' Indicates whether this category displays its resources in a stringtable
        '''   or a listview.
        ''' </summary>
        Public ReadOnly Property CategoryDisplay As Display
            Get
                Return _categoryDisplay
            End Get
        End Property

        ''' <summary>
        ''' Command to execute if you want to show this category
        ''' </summary>
        Public ReadOnly Property CommandToShow As MenuCommand
            Get
                Return _menuCommand
            End Get
        End Property

        ''' <summary>
        ''' The command used to add a new resources of appropriate type
        ''' for the current category
        ''' </summary>
        Public Property AddCommand As EventHandler
            Get
                Return _addCommand
            End Get
            Set
                _addCommand = value
            End Set
        End Property

        ''' <summary>
        ''' Returns the friendly (localized) category name, which is shown to the
        '''   user in the category buttons.
        ''' </summary>
        Public ReadOnly Property LocalizedName As String
            Get
                Return _localizedName
            End Get
        End Property

        ''' <summary>
        ''' Returns a programmatic name which is never localized and never shown to
        '''   the user.  Used when searching for a category by name key.
        ''' </summary>
        Public ReadOnly Property ProgrammaticName As String
            Get
                Return _programmaticName
            End Get
        End Property

        ''' <summary>
        ''' The number of resources currently in this category.
        ''' </summary>
        ''' <remarks>
        ''' The boldness of the category button associated with this category is automatically
        '''   updated with the resource count.  I.e., when the count is zero, the font on the
        '''   button will be normal.  When there is at least one resource in this category,
        '''   the button's font will be made bold.
        ''' </remarks>
        Public Property ResourceCount As Integer
            Get
                Return _resourceCount
            End Get
            Set
                Dim ResourcesExisted As Boolean = ResourcesExist
                _resourceCount = Value

                If _resourceCount < 0 Then
                    Debug.Fail("Resources count is less than zero for category " & _programmaticName)
                    _resourceCount = 0
                End If

                If ResourcesExisted <> ResourcesExist Then
                    RaiseEvent ResourcesExistChanged(Me, New EventArgs)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Returns true iff there are resources in this category (ResourceCount > 0)
        ''' </summary>
        Public ReadOnly Property ResourcesExist As Boolean
            Get
                Return _resourceCount > 0
            End Get
        End Property

        ''' <summary>
        ''' The current ResourceView for this category, if this category uses
        '''   a listview.  This is the "Details", "Icons", "List" option for
        '''   the display mode of the listview.
        ''' </summary>
        Public Property ResourceView As ResourceListView.ResourceView
            Get
                Return _resourceView
            End Get
            Set
                _resourceView = Value
            End Set
        End Property

        ''' <summary>
        ''' If this category uses a string table, indicates whether or not the "Type"
        '''   column is displayed (shows the type of resource, e.g. System.Drawing.Image)
        ''' </summary>
        Public Property ShowTypeColumnInStringTable As Boolean
            Get
                Return _showTypeColumnInStringTable
            End Get
            Set
                Debug.Assert(_categoryDisplay = Display.StringTable, "This property only applies to string table categories")
                _showTypeColumnInStringTable = Value
            End Set
        End Property

        ''' <summary>
        '''  how to sort the resource items in the category...
        ''' </summary>
        Public Property Sorter As IComparer(Of Resource)
            Get
                Return _sorter
            End Get
            Set
                _sorter = Value
            End Set
        End Property

        ''' <summary>
        ''' If this category uses a stringtable, indicates whether or not the user is allowed
        '''   to add new entries in this string table, via an add/new row at the bottom of
        '''   the table.
        ''' </summary>
        Public Property AllowNewEntriesInStringTable As Boolean
            Get
                Return _allowNewEntriesInStringTable
            End Get
            Set
                Debug.Assert(_categoryDisplay = Display.StringTable, "This property only applies to string table categories")
                _allowNewEntriesInStringTable = Value
            End Set
        End Property

        Public Function Compare(Resource1 As Resource, Resource2 As Resource) As Integer
            If _sorter IsNot Nothing Then
                Return _sorter.Compare(Resource1, Resource2)
            Else
                ' Name is the default order...
                Return String.Compare(Resource1.Name, Resource2.Name, ignoreCase:=True, culture:=CultureInfo.CurrentUICulture)
            End If
        End Function

    End Class
End Namespace
