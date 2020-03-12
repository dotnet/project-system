' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A warp class to warp a Common object to a component, but still keep the right properties set to show on the property Grid
    ''' </summary>
    Friend Class ComponentWrapper
        Inherits Component
        Implements ICustomTypeDescriptor

        Private _currentObject As Object

        Protected Sub New(realObject As Object)
            _currentObject = realObject
        End Sub

        ''' <summary>
        ''' the original object
        ''' </summary>
        Protected Friend Property CurrentObject As Object
            Get
                Return _currentObject
            End Get
            Set
                Debug.Assert(value IsNot Nothing, "can not support Nothing")
                _currentObject = value
            End Set
        End Property

        Public Function GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(_currentObject)
        End Function

        Public Function GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            Return TypeDescriptor.GetClassName(_currentObject)
        End Function

        Public Function GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            Return TypeDescriptor.GetComponentName(_currentObject)
        End Function

        Public Function GetConverter() As TypeConverter Implements ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter(_currentObject)
        End Function

        Public Function GetDefaultEvent() As EventDescriptor Implements ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent(_currentObject)
        End Function

        Public Function GetDefaultProperty() As PropertyDescriptor Implements ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty(_currentObject)
        End Function

        Public Function GetEditor(editorBaseType As Type) As Object Implements ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor(_currentObject, editorBaseType)
        End Function

        Public Function GetEvents() As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(_currentObject)
        End Function

        Public Function GetEvents1(attributes() As Attribute) As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(_currentObject, attributes)
        End Function

        Public Function GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(_currentObject)
        End Function

        Public Function GetProperties1(attributes() As Attribute) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties(_currentObject, attributes)
        End Function

        Public Function GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            Return _currentObject
        End Function
    End Class

End Namespace

