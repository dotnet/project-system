' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data

Namespace Microsoft.VisualStudio.Editors.OptionPages
    ' This code is based on corresponding functionality in dotnet\roslyn: https://github.com/dotnet/roslyn/blob/master/src/VisualStudio/Core/Impl/Options/AbstractOptionPageControl.cs

    Friend Class OptionPageControl
        Inherits UserControl

        Private ReadOnly _bindingExpressions As New List(Of BindingExpressionBase)()

        Public Sub New()
            Dim labelStyle = New Style(GetType(Label))
            labelStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            labelStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=7, right:=0, bottom:=7)))
            Resources.Add(GetType(Label), labelStyle)

            Dim checkBoxStyle = New Style(GetType(CheckBox))
            checkBoxStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            checkBoxStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=7, right:=0, bottom:=7)))
            Resources.Add(GetType(CheckBox), checkBoxStyle)
        End Sub

        Protected Sub AddBinding(bindingExpression As BindingExpressionBase)
            _bindingExpressions.Add(bindingExpression)
        End Sub

        Friend Overridable Sub LoadSettings()
            For Each bindingExpression In _bindingExpressions
                bindingExpression.UpdateTarget()
            Next
        End Sub

        Friend Overridable Sub SaveSettings()
            For Each bindingExpression In _bindingExpressions
                If (Not bindingExpression.IsDirty) Then
                    Continue For
                End If

                bindingExpression.UpdateSource()
            Next
        End Sub

        Friend Overridable Sub Close()
        End Sub
    End Class
End Namespace