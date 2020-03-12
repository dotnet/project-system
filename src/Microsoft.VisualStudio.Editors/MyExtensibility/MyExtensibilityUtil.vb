' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.Reflection
Imports System.Xml

Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensibilityUtil
    ''' <summary>
    ''' Common utility methods for MyExtensibility.
    ''' </summary>
    Friend Class MyExtensibilityUtil

        ''' ;StringEquals
        ''' <summary>
        ''' Perform OrdinalIgnoreCase string comparison.
        ''' </summary>
        Public Shared Function StringEquals(s1 As String, s2 As String) As Boolean
            Return String.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' ;StringIsNullEmptyOrBlank
        ''' <summary>
        ''' Check if the given string is null, empty or all blank spaces.
        ''' </summary>
        Public Shared Function StringIsNullEmptyOrBlank(s As String) As Boolean
            Return String.IsNullOrEmpty(s) OrElse s.Trim().Length = 0
        End Function

        ''' ;GetAttributeValue
        ''' <summary>
        ''' Get the trimmed attribute with the given name from the given xml element.
        ''' Return Nothing if such attributes don't exist.
        ''' </summary>
        Public Shared Function GetAttributeValue(xmlElement As XmlElement, attributeName As String) _
                As String
            Dim xmlAttribute As XmlAttribute = xmlElement.Attributes(attributeName)

            If xmlAttribute IsNot Nothing AndAlso xmlAttribute.Value IsNot Nothing Then
                Return xmlAttribute.Value.Trim()
            End If

            Return Nothing
        End Function

        ''' ;GerVersion
        ''' <summary>
        ''' Construct a Version instance from the given string, return Version(0.0.0.0)
        ''' if the string format is incorrect.
        ''' </summary>
        Public Shared Function GetVersion(versionString As String) As Version
            Dim result As New Version(0, 0, 0, 0)
            If Not String.IsNullOrEmpty(versionString) Then
                Try
                    result = New Version(versionString)
                Catch ex As ArgumentException ' Ignore exceptions from version constructor.
                Catch ex As FormatException
                Catch ex As OverflowException
                End Try
            End If
            Return result
        End Function

        ''' ;NormalizeAssemblyFullName
        ''' <summary>
        ''' Given an assembly full name, return a full name containing only name and version.
        ''' </summary>
        Public Shared Function NormalizeAssemblyFullName(assemblyFullName As String) As String
            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                Return Nothing
            End If
            Try
                Dim inputAsmName As New AssemblyName(assemblyFullName)
                Dim outputAsmName As New AssemblyName With {
                    .Name = inputAsmName.Name,
                    .Version = inputAsmName.Version
                }
                Return outputAsmName.FullName
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(NormalizeAssemblyFullName), NameOf(MyExtensibilityUtil))
                Return Nothing
            End Try
        End Function
    End Class

    Friend Enum AddRemoveAction As Byte
        Add = 1
        Remove = 0
    End Enum

    ''' ;AssemblyOption
    ''' <summary>
    ''' Auto-add or auto-remove option:
    ''' - Yes: Silently add or remove the extensions triggered by the assembly.
    ''' - No: Do not add or remove extensions triggered by the assembly. 
    ''' - Prompt: Prompt user.
    ''' </summary>
    Friend Enum AssemblyOption
        No = 0
        Yes = 1
        Prompt = 2
    End Enum

    ''' ;AssemblyDictionary
    ''' <summary>
    ''' A dictionary based on assembly full name. It contains a list of assembly independent items and
    ''' a dictionary of assembly name and (dictionary of version and item). When version with an assembly name
    ''' it will return the list of items corresponding to that name.
    ''' </summary>
    Friend Class AssemblyDictionary(Of T)

        Public Sub New()
        End Sub

        ''' <summary>
        ''' Add the given item with the given assemblyFullName to the dictionary.
        ''' </summary>
        Public Sub AddItem(assemblyFullName As String, item As T)
            If item Is Nothing Then
                Exit Sub
            End If

            Dim assemblyName As String = Nothing
            Dim assemblyVersion As Version = Nothing
            ParseAssemblyFullName(assemblyFullName, assemblyName, assemblyVersion)

            If assemblyName Is Nothing Then
                If _assemblyIndependentList Is Nothing Then
                    _assemblyIndependentList = New List(Of T)()
                End If
                _assemblyIndependentList.Add(item)
            Else
                If _assemblyDictionary Is Nothing Then
                    _assemblyDictionary = New Dictionary(Of String, AssemblyVersionDictionary(Of T))(
                        StringComparer.OrdinalIgnoreCase)
                End If

                Dim asmVersionDictionary As AssemblyVersionDictionary(Of T)

                If _assemblyDictionary.ContainsKey(assemblyName) Then
                    asmVersionDictionary = _assemblyDictionary(assemblyName)
                Else
                    asmVersionDictionary = New AssemblyVersionDictionary(Of T)
                    _assemblyDictionary.Add(assemblyName, asmVersionDictionary)
                End If
                asmVersionDictionary.AddItem(assemblyVersion, item)
            End If
        End Sub

        ''' ;RemoveItem
        ''' <summary>
        ''' Remove the given item from the dictionary.
        ''' </summary>
        Public Sub RemoveItem(item As T)
            If item Is Nothing Then
                Exit Sub
            End If
            If _assemblyIndependentList IsNot Nothing AndAlso _assemblyIndependentList.Contains(item) Then
                _assemblyIndependentList.Remove(item)
            End If
            If _assemblyDictionary IsNot Nothing AndAlso _assemblyDictionary.Values.Count > 0 Then
                For Each versionDict As AssemblyVersionDictionary(Of T) In _assemblyDictionary.Values
                    versionDict.RemoveItem(item)
                Next
            End If
        End Sub

        ''' <summary>
        ''' Get a list of item with the given assembly full name from the dictionary.
        ''' </summary>
        Public Function GetItems(assemblyFullName As String) As List(Of T)
            Dim assemblyName As String = Nothing
            Dim assemblyVersion As Version = Nothing
            ParseAssemblyFullName(assemblyFullName, assemblyName, assemblyVersion)

            If assemblyName Is Nothing Then
                Return _assemblyIndependentList
            Else
                If _assemblyDictionary IsNot Nothing AndAlso _assemblyDictionary.ContainsKey(assemblyName) Then
                    Return _assemblyDictionary(assemblyName).GetItems(assemblyVersion)
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Get all items contained in the dictionary. Return either NULL or a list with something.
        ''' </summary>
        Public Function GetAllItems() As List(Of T)
            Dim result As New List(Of T)

            If _assemblyIndependentList IsNot Nothing AndAlso _assemblyIndependentList.Count > 0 Then
                result.AddRange(_assemblyIndependentList)
            End If

            If _assemblyDictionary IsNot Nothing Then
                For Each asmVersionDictionary As AssemblyVersionDictionary(Of T) In _assemblyDictionary.Values
                    Dim versionDependentItems As List(Of T) = asmVersionDictionary.GetAllItems()
                    If versionDependentItems IsNot Nothing AndAlso versionDependentItems.Count > 0 Then
                        result.AddRange(versionDependentItems)
                    End If
                Next
            End If

            If result.Count <= 0 Then
                result = Nothing
            End If
            Return result
        End Function

        Public Sub Clear()
            If _assemblyIndependentList IsNot Nothing Then
                _assemblyIndependentList.Clear()
            End If
            If _assemblyDictionary IsNot Nothing Then
                _assemblyDictionary.Clear()
            End If
        End Sub

        Private Sub ParseAssemblyFullName(assemblyFullName As String,
                ByRef assemblyName As String, ByRef assemblyVersion As Version)

            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                assemblyName = Nothing
                assemblyVersion = Nothing
            Else
                Try
                    Dim asmName As New AssemblyName(assemblyFullName)
                    assemblyName = asmName.Name
                    assemblyVersion = asmName.Version
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ParseAssemblyFullName), NameOf(MyExtensibilityUtil))
                    assemblyName = Nothing
                    assemblyVersion = Nothing
                End Try
            End If
        End Sub

        Private _assemblyIndependentList As List(Of T)
        Private _assemblyDictionary As Dictionary(Of String, AssemblyVersionDictionary(Of T))

        ''' <summary>
        ''' A dictionary based on assembly version. It contains a list of version independent items
        ''' and a dictionary of version dependent items. When query with a version it will return
        ''' a list of version independent items and items of the correct version if applicable.
        ''' </summary>
        Private Class AssemblyVersionDictionary(Of Y)

            ''' <summary>
            ''' Add an item with the given version to the dictionary.
            ''' </summary>
            Public Sub AddItem(version As Version, item As Y)
                If item Is Nothing Then
                    Exit Sub
                End If
                If version Is Nothing Then
                    If _versionIndependentList Is Nothing Then
                        _versionIndependentList = New List(Of Y)
                    End If
                    _versionIndependentList.Add(item)
                Else

                    If _versionDependentDictionary Is Nothing Then
                        _versionDependentDictionary = New Dictionary(Of Version, List(Of Y))()
                    End If

                    Dim itemList As List(Of Y)

                    If _versionDependentDictionary.ContainsKey(version) Then
                        itemList = _versionDependentDictionary(version)
                    Else
                        itemList = New List(Of Y)
                        _versionDependentDictionary.Add(version, itemList)
                    End If
                    itemList.Add(item)
                End If
            End Sub

            ''' <summary>
            ''' Get a list of items for the given version.
            ''' </summary>
            Public Function GetItems(version As Version) As List(Of Y)
                Dim result As New List(Of Y)

                ' Always include version independent list.
                If _versionIndependentList IsNot Nothing AndAlso _versionIndependentList.Count > 0 Then
                    result.AddRange(_versionIndependentList)
                End If

                If version IsNot Nothing Then ' Include the version dependent list if applicable
                    If _versionDependentDictionary IsNot Nothing AndAlso _versionDependentDictionary.ContainsKey(version) Then
                        Dim itemList As List(Of Y) = _versionDependentDictionary(version)
                        If itemList IsNot Nothing AndAlso itemList.Count > 0 Then
                            result.AddRange(itemList)
                        End If
                    End If
                End If

                If result.Count <= 0 Then
                    result = Nothing
                End If
                Return result
            End Function

            ''' <summary>
            ''' Get all items available.
            ''' </summary>
            Public Function GetAllItems() As List(Of Y)
                Dim result As New List(Of Y)

                If _versionIndependentList IsNot Nothing AndAlso _versionIndependentList.Count > 0 Then
                    result.AddRange(_versionIndependentList)
                End If

                If _versionDependentDictionary IsNot Nothing Then
                    For Each itemList As List(Of Y) In _versionDependentDictionary.Values
                        result.AddRange(itemList)
                    Next
                End If

                If result.Count <= 0 Then
                    result = Nothing
                End If
                Return result
            End Function

            ''' ;RemoveItem
            ''' <summary>
            ''' Remove an item from the dictionary.
            ''' </summary>
            Public Sub RemoveItem(item As Y)
                If item Is Nothing Then
                    Exit Sub
                End If
                If _versionIndependentList IsNot Nothing AndAlso _versionIndependentList.Contains(item) Then
                    _versionIndependentList.Remove(item)
                End If
                If _versionDependentDictionary IsNot Nothing AndAlso
                        _versionDependentDictionary.Values IsNot Nothing Then
                    For Each itemList As List(Of Y) In _versionDependentDictionary.Values
                        If itemList.Contains(item) Then
                            itemList.Remove(item)
                        End If
                    Next
                End If
            End Sub

            Public Sub Clear()
                If _versionIndependentList IsNot Nothing Then
                    _versionIndependentList.Clear()
                End If
                If _versionDependentDictionary IsNot Nothing Then
                    _versionDependentDictionary.Clear()
                End If
            End Sub

            Private _versionIndependentList As List(Of Y)
            Private _versionDependentDictionary As Dictionary(Of Version, List(Of Y))
        End Class
    End Class
End Namespace
