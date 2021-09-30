' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Wrapper class for the settings type resolver that caches previously resolved
    ''' type names.
    ''' </summary>
    Friend Class SettingsTypeCache
        Implements IDisposable

        Private _multiTargetService As MultiTargetService
        Private _typeResolutionService As ComponentModel.Design.ITypeResolutionService
        Private ReadOnly _caseSensitive As Boolean

        ' The list of types that we always know how to find. 
        Private ReadOnly _wellKnownTypes() As Type = {
                                                    GetType(Boolean),
                                                    GetType(Byte),
                                                    GetType(Char),
                                                    GetType(Date),
                                                    GetType(Decimal),
                                                    GetType(Double),
                                                    GetType(Guid),
                                                    GetType(Short),
                                                    GetType(Integer),
                                                    GetType(Long),
                                                    GetType(SByte),
                                                    GetType(Single),
                                                    GetType(TimeSpan),
                                                    GetType(UShort),
                                                    GetType(UInteger),
                                                    GetType(ULong),
                                                    GetType(Drawing.Color),
                                                    GetType(Drawing.Font),
                                                    GetType(Drawing.Point),
                                                    GetType(Drawing.Size),
                                                    GetType(String),
                                                    GetType(Specialized.StringCollection)
                                                            }

        ''' <summary>
        ''' Given a "normal" ITypeResolutionService, create an instance of the settingstypecache
        ''' </summary>
        ''' <param name="typeResolutionService"></param>
        Public Sub New(vsHierarchy As IVsHierarchy, ItemId As UInteger, typeResolutionService As ComponentModel.Design.ITypeResolutionService, caseSensitive As Boolean)
            If typeResolutionService Is Nothing OrElse vsHierarchy Is Nothing Then
                Debug.Fail("We really need a type resolution service or IVsHierarchy in order to do anything interesting!")
                Throw New ArgumentNullException()
            End If

            _multiTargetService = New MultiTargetService(vsHierarchy, ItemId, False)
            _typeResolutionService = typeResolutionService
            _caseSensitive = caseSensitive
        End Sub

        Private Sub Dispose(disposing As Boolean)
            If disposing Then
                If _multiTargetService IsNot Nothing Then
                    _multiTargetService.Dispose()
                    _multiTargetService = Nothing
                End If
                _typeResolutionService = Nothing
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Get the type corresponding to the given type name. If the type was found in the cache, but
        ''' was obsolete:d (the assembly containing the type has changed) it is removed from the cache.
        ''' 
        ''' The cache is updated with the returned type to improve performance of subsequent resolves...
        ''' </summary>
        ''' <param name="typeName"></param>
        Public Function GetSettingType(typeName As String) As Type
            ' First, check our list of well known types...
            For Each wellKnownType As Type In GetWellKnownTypes()
                If String.Equals(wellKnownType.FullName, typeName) Then
                    Return wellKnownType
                End If
            Next
            Return ResolveType(typeName, _caseSensitive)
        End Function

        ''' <summary>
        ''' Get the list of "well known" types (i.e. types that we don't need any type resolution service
        ''' in order to resolve...
        ''' </summary>
        Public Function GetWellKnownTypes() As Type()
            Return _multiTargetService.GetSupportedTypes(_wellKnownTypes)
        End Function

        ''' <summary>
        ''' Is the given type a well known type?
        ''' </summary>
        ''' <param name="type"></param>
        Public Function IsWellKnownType(type As Type) As Boolean
            For Each wellKnownType As Type In GetWellKnownTypes()
                If wellKnownType Is type Then
                    Return True
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' Private implementation that adds logic to understand our own "virtual" connection string
        ''' and web service URL types
        ''' </summary>
        ''' <param name="persistedSettingTypeName"></param>
        ''' <param name="caseSensitive"></param>
        Private Function ResolveType(persistedSettingTypeName As String, caseSensitive As Boolean) As Type
            Dim t As Type

            If String.Equals(persistedSettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return GetType(VSDesignerPackage.SerializableConnectionString)
            ElseIf String.Equals(persistedSettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                t = GetType(String)
            Else
                t = _typeResolutionService.GetType(persistedSettingTypeName, False, Not caseSensitive)
            End If

            If t IsNot Nothing Then
                Return _multiTargetService.GetSupportedType(t, False)
            Else
                Return t
            End If
        End Function

        Public Function TypeTransformer(sourceTypeName As String) As String
            Dim qualifiedAssemblyName As String = Nothing

            If Not String.IsNullOrEmpty(sourceTypeName) Then

                ' The configuration types represent a stable contract, for .NET scenarios pin all these type names to the .NET Framework identities.
                ' This will ensure that when we create required sections in app.config we'll be able to read them back.
                ' Note that when the designer moves off of .NET Framework we may need to revisit this.
                If (_multiTargetService.TargetFrameworkName.Identifier = ".NETCoreApp" AndAlso
                   (sourceTypeName = "System.Configuration.ApplicationSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" Or
                    sourceTypeName = "System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" Or
                    sourceTypeName = "System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")) Then
                    Return sourceTypeName
                End If

                Dim t As Type = _typeResolutionService.GetType(sourceTypeName, False, Not _caseSensitive)
                If t IsNot Nothing Then
                    qualifiedAssemblyName = _multiTargetService.TypeNameConverter(t)
                End If
            End If

            Return qualifiedAssemblyName
        End Function

    End Class

End Namespace
