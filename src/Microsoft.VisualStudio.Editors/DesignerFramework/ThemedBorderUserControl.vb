' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles

Namespace Microsoft.VisualStudio.Editors.DesignerFramework
    Friend Class ThemedBorderUserControl
        Private _borderPen As Pen
        Private Const WS_EX_CLIENTEDGE As Integer = &H200
        Private Const WS_BORDER As Integer = &H800000

        Public Sub New()
            _borderPen = New Pen(VisualStyleInformation.TextControlBorder)
        End Sub

        Protected Overrides ReadOnly Property CreateParams As CreateParams
            Get
                Dim cp As CreateParams = MyBase.CreateParams
                cp.ExStyle = cp.ExStyle And Not WS_EX_CLIENTEDGE
                cp.Style = cp.Style And Not WS_BORDER
                If Not UseVisualStyles Then

                    Select Case BorderStyle
                        Case BorderStyle.Fixed3D
                            cp.ExStyle = cp.ExStyle Or WS_EX_CLIENTEDGE
                        Case BorderStyle.FixedSingle
                            cp.Style = cp.Style Or WS_BORDER
                    End Select
                End If
                Return cp
            End Get
        End Property

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
                _borderPen.Dispose()
            End If

            MyBase.Dispose(disposing)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            ' we have to get a new pen every time
            _borderPen.Dispose()
            _borderPen = New Pen(VisualStyleInformation.TextControlBorder)
            If UseVisualStyles Then
                If Me.BorderStyle = Windows.Forms.BorderStyle.Fixed3D Then
                    e.Graphics.DrawRectangle(_borderPen, New Rectangle(0, 0, Width - 1, Height - 1))
                End If
            End If
            MyBase.OnPaint(e)
        End Sub

        Protected Overrides ReadOnly Property DefaultPadding As Padding
            Get
                Return New Padding(1)
            End Get
        End Property

        Private Shared ReadOnly Property UseVisualStyles As Boolean
            Get
                Return VisualStyleRenderer.IsSupported
            End Get
        End Property

    End Class
End Namespace
