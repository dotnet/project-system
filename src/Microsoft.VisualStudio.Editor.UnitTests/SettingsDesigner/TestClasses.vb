' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace TestClasses

    Public Class XmlSerializableTestClass

        Private m_bar As String = "Zoo"

        Public Property Bar() As String
            Get
                Return m_bar
            End Get
            Set(ByVal value As String)
                m_bar = value
            End Set
        End Property
    End Class

    Friend Class PrivateTestObject

        Private newPropertyValue As Integer
        Public Property NewProperty() As Integer
            Get
                Return newPropertyValue
            End Get
            Set(ByVal value As Integer)
                newPropertyValue = value
            End Set
        End Property

    End Class
End Namespace
