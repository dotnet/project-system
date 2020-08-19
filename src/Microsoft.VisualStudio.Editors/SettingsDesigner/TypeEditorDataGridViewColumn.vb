' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' UI Type editor column for DataGridView
    ''' </summary>
    Friend NotInheritable Class DataGridViewUITypeEditorColumn
        Inherits DataGridViewColumn

        Public Sub New()
            MyBase.New(New DataGridViewUITypeEditorCell)
        End Sub

        Public Overrides Property CellTemplate As DataGridViewCell
            Get
                Return MyBase.CellTemplate
            End Get
            Set
                If Value IsNot Nothing AndAlso TypeOf Value IsNot DataGridViewUITypeEditorCell Then
                    Throw New InvalidCastException()
                End If

                MyBase.CellTemplate = Value
            End Set
        End Property
    End Class
End Namespace
