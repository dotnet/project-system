' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The component class to wrap the original reference object. We will push this object to the property grid
    ''' </summary>
    Friend Class ReferenceComponent
        Inherits ComponentWrapper
        Implements IComparable, IReferenceComponent

        Public Sub New(realObject As VSLangProj.Reference)
            MyBase.New(realObject)
        End Sub

        ''' <summary>
        ''' The original reference object in DTE.Project
        ''' </summary>
        Friend ReadOnly Property CodeReference As VSLangProj.Reference
            Get
                Return CType(CurrentObject, VSLangProj.Reference)
            End Get
        End Property

        Friend ReadOnly Property Name As String
            Get
                Return CodeReference.Name
            End Get
        End Property

        ''' <summary>
        ''' Remove the reference from the project...
        ''' </summary>
        Private Sub Remove() Implements IReferenceComponent.Remove
            CodeReference.Remove()
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return Name
        End Function

        Public Function CompareTo(obj As Object) As Integer Implements IComparable.CompareTo
            Dim y As ReferenceComponent = CType(obj, ReferenceComponent)
            If y IsNot Nothing Then
                Return String.Compare(Name, y.Name)
            Else
                Debug.Fail("we can not compare to an unknown object")
                Return 1
            End If
        End Function
    End Class

End Namespace

