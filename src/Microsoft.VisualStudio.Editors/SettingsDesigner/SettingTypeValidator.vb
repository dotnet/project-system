' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Manages the decisions on if a type is valid as a setting type and/or
    ''' is obsolete
    ''' </summary>
    Friend Class SettingTypeValidator

        ''' <summary>
        ''' Indicate if the type has changed from the version that is currently loaded in this app domain
        ''' </summary>
        ''' <param name="type"></param>
        Public Shared Function IsTypeObsolete(type As Type) As Boolean
            Dim typeInLoadedAssembly As Type = Type.GetType(type.AssemblyQualifiedName)

            ' If the type we get from System.Type.GetType(<assembly qualified type name>) is not the same
            ' as the type provided, something has changed in the defining assembly
            Return ReferenceEquals(type, typeInLoadedAssembly) = False
        End Function

        Public Shared Function IsValidSettingType(type As Type) As Boolean

            If Not type.IsPublic Then Return False
            If type.IsPointer Then Return False
            If type.IsGenericType Then Return False
            If type Is GetType(Void) Then Return False
            If Not (type.IsClass OrElse type.IsValueType) Then Return False
            If Not CanSerializeType(type) Then Return False

            Return True
        End Function

        Private Shared Function CanSerializeType(type As Type) As Boolean
            Try
                Dim tc As TypeConverter = TypeDescriptor.GetConverter(type)
                If tc.CanConvertFrom(GetType(String)) AndAlso tc.CanConvertTo(GetType(String)) Then
                    Return True
                End If
                If type.GetConstructor(BindingFlags.Instance Or BindingFlags.Public, Nothing, CallingConventions.HasThis, Type.EmptyTypes, Nothing) IsNot Nothing Then
                    Return True
                End If
            Catch ex As Exception
                ' Since we have no idea what the custom type descriptors may do, we catch everything that we can and 
                ' pretend that nothing too bad happened...
            End Try
            Return False
        End Function

    End Class
End Namespace
