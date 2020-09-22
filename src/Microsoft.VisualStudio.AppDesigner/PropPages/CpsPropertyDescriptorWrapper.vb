' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Wraps a CPS <see cref="PropertyDescriptor"/>, so the correct <see cref="PropertyDescriptor"/>
    ''' gets used when getting/setting values when bound to multiple components.
    ''' </summary>
    Public Class CpsPropertyDescriptorWrapper
        Inherits PropertyDescriptor

        Private ReadOnly _defaultDescriptor As PropertyDescriptor

        Public Sub New(defaultDescriptor As PropertyDescriptor)
            MyBase.New(defaultDescriptor.Name, Array.Empty(Of Attribute)())
            _defaultDescriptor = defaultDescriptor
        End Sub

        Public Overrides Function CanResetValue(component As Object) As Boolean
            Return GetComponentPropertyDescriptor(component).CanResetValue(component)
        End Function

        Public Overrides Sub ResetValue(component As Object)
            GetComponentPropertyDescriptor(component).ResetValue(component)
        End Sub

        Public Overrides Sub SetValue(component As Object, value As Object)
            GetComponentPropertyDescriptor(component).SetValue(component, value)

            ' force value changed event to fire (CPS descriptors do not support events, but PropertyControlData requires them)
            MyBase.OnValueChanged(component, New EventArgs())
        End Sub

        Public Overrides Function GetValue(component As Object) As Object
            Return GetComponentPropertyDescriptor(component).GetValue(component)
        End Function

        Public Overrides Function ShouldSerializeValue(component As Object) As Boolean
            Return GetComponentPropertyDescriptor(component).ShouldSerializeValue(component)
        End Function

        Public Overrides Function GetChildProperties(instance As Object, filter() As Attribute) As PropertyDescriptorCollection
            Return GetComponentPropertyDescriptor(instance).GetChildProperties(instance, filter)
        End Function

        Public Overrides ReadOnly Property SupportsChangeEvents As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Gets the actual CPS <see cref="PropertyDescriptor"/> associated the the component
        ''' </summary>
        Private Function GetComponentPropertyDescriptor(component As Object) As PropertyDescriptor
            Dim typeDescriptor = TryCast(component, ICustomTypeDescriptor)
            If typeDescriptor IsNot Nothing Then
                Dim descriptor = typeDescriptor.GetProperties().OfType(Of PropertyDescriptor)().FirstOrDefault(Function(pd) pd.Name = _defaultDescriptor.Name)
                If descriptor IsNot Nothing Then
                    Return descriptor
                End If
            End If
            Return _defaultDescriptor
        End Function

#Region "Wrappers"
        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return _defaultDescriptor.ComponentType
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _defaultDescriptor.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property PropertyType As Type
            Get
                Return _defaultDescriptor.PropertyType
            End Get
        End Property

        Public Overrides ReadOnly Property Attributes As AttributeCollection
            Get
                Return _defaultDescriptor.Attributes
            End Get
        End Property

        Public Overrides ReadOnly Property Category As String
            Get
                Return _defaultDescriptor.Category
            End Get
        End Property

        Public Overrides ReadOnly Property Converter As TypeConverter
            Get
                Return _defaultDescriptor.Converter
            End Get
        End Property

        Public Overrides ReadOnly Property Description As String
            Get
                Return _defaultDescriptor.Description
            End Get
        End Property

        Public Overrides ReadOnly Property DesignTimeOnly As Boolean
            Get
                Return _defaultDescriptor.DesignTimeOnly
            End Get
        End Property

        Public Overrides ReadOnly Property DisplayName As String
            Get
                Return _defaultDescriptor.DisplayName
            End Get
        End Property

        Public Overrides Function GetEditor(editorBaseType As Type) As Object
            Return _defaultDescriptor.GetEditor(editorBaseType)
        End Function

        Public Overrides ReadOnly Property IsBrowsable As Boolean
            Get
                Return _defaultDescriptor.IsBrowsable
            End Get
        End Property

        Public Overrides ReadOnly Property IsLocalizable As Boolean
            Get
                Return _defaultDescriptor.IsLocalizable
            End Get
        End Property
#End Region

        Public Shared Function IsAnyCpsComponent(components() As Object) As Boolean
            Return components.Any(Function(c) IsCpsComponent(c))
        End Function

        Public Shared Function IsCpsComponent(component As Object) As Boolean
            Return component.GetType().Namespace.StartsWith("Microsoft.VisualStudio.ProjectSystem.VS")
        End Function

    End Class
End Namespace
