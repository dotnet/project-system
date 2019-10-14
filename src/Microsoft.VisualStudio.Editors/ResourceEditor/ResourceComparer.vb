' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Globalization

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is an <see cref="IComparer(Of T)"/> implementation used to sort <see cref="Resource"/>s for UI purposes (ResourceListView and
    '''    ResourceStringTable).
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceComparer
        Implements IComparer(Of Resource)

        ''' <summary>
        ''' Sorts a <see cref="List(Of Resource)"/> for UI purposes
        ''' </summary>
        ''' <param name="Resources"><see cref="List(Of Resource)"/> to sort (will be sorted in place)</param>
        ''' <remarks></remarks>
        Public Shared Sub SortResources(Resources As List(Of Resource))
            Resources.Sort(New ResourceComparer)
        End Sub

        ''' <summary>
        ''' Compares two objects and returns a value indicating whether one is less than, equal to or greater than the other.
        ''' </summary>
        ''' <param name="x">First object to compare.</param>
        ''' <param name="y">Second object to compare.</param>
        ''' <returns>-1, 0 or 1, depending on whether x is less than, equal to or greater than y, respectively.</returns>
        ''' <remarks>This function gets called by ArrayList.Sort for each pair of resources to be sorted.</remarks>
        Private Function Compare(x As Resource, y As Resource) As Integer Implements IComparer(Of Resource).Compare
            'We currently only support sorting alphabetically according to Name.
            Return String.Compare(x.Name, y.Name, ignoreCase:=True, culture:=CultureInfo.CurrentUICulture)
        End Function
    End Class

End Namespace
