' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    '''  a sub class of DescriptionAttribute to help localizing the description...
    ''' </summary>
    <AttributeUsage(AttributeTargets.All)>
    Friend Class VBDescriptionAttribute
        Inherits DescriptionAttribute

        Private _replaced As Boolean

        Public Sub New(description As String)
            MyBase.New(description)
        End Sub

        Public Overrides ReadOnly Property Description As String
            Get
                If Not _replaced Then
                    _replaced = True
                    DescriptionValue = My.Resources.Microsoft_VisualStudio_Editors_Designer.ResourceManager.GetString(DescriptionValue)
                End If
                Return DescriptionValue
            End Get
        End Property
    End Class

End Namespace

