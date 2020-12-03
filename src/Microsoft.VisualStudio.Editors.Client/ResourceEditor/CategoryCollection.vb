' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A strongly-typed collection of Category, allowing indexing by both
    '''   index and string (programmatic name as key)
    ''' </summary>
    ''' <remarks>Inherits from CollectionBase, from which it inherits an ArrayList "InnerList"
    '''   indexable by integer</remarks>
    Friend NotInheritable Class CategoryCollection
        Inherits CollectionBase

        'A hashtable list of resources by name.
        Private ReadOnly _innerHashByName As New Dictionary(Of String, Category)

        '======================================================================
        '= Properties =                                                       =
        '======================================================================

        ''' <summary>
        ''' Searches for a category by its index
        ''' </summary>
        ''' <param name="Index">The integer index to look up</param>
        ''' <remarks>Throws an exception if out of bounds.</remarks>
        Default Public ReadOnly Property Item(Index As Integer) As Category
            Get
                Return DirectCast(InnerList(Index), Category)
            End Get
        End Property

        ''' <summary>
        ''' Searches for a category by its programmatic name.
        ''' </summary>
        ''' <param name="ProgrammaticCategoryName">Category name to search for.</param>
        ''' <value>The category if found, or else Nothing.</value>
        ''' <remarks>Does not throw an exception if not found.</remarks>
        Default Public ReadOnly Property Item(ProgrammaticCategoryName As String) As Category
            Get
                Dim Value As Category = Nothing
                _innerHashByName.TryGetValue(ProgrammaticCategoryName, Value)
                Return Value
            End Get
        End Property

        '======================================================================
        '= Methods =                                                          =
        '======================================================================

        ''' <summary>
        ''' Adds a category to the collection.
        ''' </summary>
        ''' <param name="Category">The category to add.</param>
        Public Sub Add(Category As Category)
            'Add to the inner list (indexable by integer index)
            InnerList.Add(Category)

            'Add to the hash table (for indexing by programmatic name)
            _innerHashByName(Category.ProgrammaticName) = Category
        End Sub

    End Class

End Namespace
