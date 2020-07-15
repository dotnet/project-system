' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public NotInheritable Class ApplicationDesignerWindowPaneControl
        Inherits UserControl

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Selects the next available control and makes it the active control.
        ''' </summary>
        ''' <param name="forward"></param>
        Protected Overrides Function ProcessTabKey(forward As Boolean) As Boolean
            Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "ApplicationDesignerWindowPaneControl.ProcessTabKey")

            If SelectNextControl(ActiveControl, forward, True, True, False) Then
                Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ...SelectNextControl handled it")
                Return True
            End If

            Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ...Not handled")
            Return False
        End Function

        Private Sub InitializeComponent()
            '
            'ApplicationDesignerWindowPaneControl
            '
            Name = "ApplicationDesignerWindowPaneControl"
            Text = "ApplicationDesignerWindowPaneControl" 'For debugging

            'We don't want scrollbars to show up on this window
            AutoScroll = False

        End Sub

#If DEBUG Then
        Private Sub ApplicationDesignerWindowPaneControl_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged
            Common.Switches.TracePDFocus(TraceLevel.Info, "ApplicationDesignerWindowPaneControl_SizeChanged: " & Size.ToString())
        End Sub
#End If

    End Class

End Namespace
