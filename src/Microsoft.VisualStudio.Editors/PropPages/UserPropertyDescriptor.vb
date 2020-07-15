' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A very simple property descriptor class for user-defined properties handled directly by the page 
    '''   (PropertyControlData.IsUserPersisted = True).
    ''' Should be returned by an overriden GetUserDefinedPropertyDescriptor.
    ''' This is used mainly in integrating with the Undo/Redo capabilities.
    ''' </summary>
    Public Class UserPropertyDescriptor
        Inherits PropertyDescriptor

        Private ReadOnly _propertyType As Type
        Private ReadOnly _isReadOnly As Boolean

        Public Sub New(Name As String, PropertyType As Type)
            MyBase.New(Name, Array.Empty(Of Attribute))
            _propertyType = PropertyType
        End Sub

        Public Overrides Function CanResetValue(component As Object) As Boolean
            Return False
        End Function

        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetValue(component As Object) As Object
            'Note: this function never gets called and does not need to be implemented (the call is
            '  intercepted by the project designer)
            Debug.Fail("This should not get called")
            Return Nothing
        End Function

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _isReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property PropertyType As Type
            Get
                Return _propertyType
            End Get
        End Property

        Public Overrides Sub ResetValue(component As Object)
        End Sub

        Public Overrides Sub SetValue(component As Object, value As Object)
            'Note: this function never gets called and does not need to be implemented (the call is
            '  intercepted by the project designer)
            Debug.Fail("This should not get called")
        End Sub

        Public Overrides Function ShouldSerializeValue(component As Object) As Boolean
            Return True
        End Function

    End Class

End Namespace

