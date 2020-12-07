' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' In case we're building an editor, the editor's view will contain some user controls built from FX.
    '''   These user controls handles context menu in a different way. To show the context menu the correct way,
    '''   we inherit from the FX's control and override their WndProc.
    '''   
    '''   DesignerListView is our control inherited from ListView.
    ''' </summary>
    Friend Class DesignerListView
        Inherits ListView

        ''' <summary>
        ''' ContextMenuShow will be raised when this list view needs to show its context menu.
        ''' The derived control simply needs to handle this event to know when to show a
        '''   context menu
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Public Event ContextMenuShow(sender As Object, e As MouseEventArgs)

        ''' <summary>
        ''' We override Control.WndProc to raise the ContextMenuShow event.
        ''' </summary>
        ''' <param name="m">Windows message passed in by window.</param>
        ''' <remarks>Implementation based on sources\ndp\fx\src\WinForms\Managed\System\WinForms\Control.cs</remarks>
        Protected Overrides Sub WndProc(ByRef m As Message)
            ' We only handle the context menu specially.
            Select Case m.Msg
                Case Interop.Win32Constant.WM_CONTEXTMENU
                    Debug.WriteLineIf(Switches.DFContextMenu.TraceVerbose, "WM_CONTEXTMENU")

                    Dim EventArgs As MouseEventArgs = DesignUtil.GetContextMenuMouseEventArgs(m)
                    RaiseEvent ContextMenuShow(Me, EventArgs)
                Case Else
                    MyBase.WndProc(m)
            End Select
        End Sub

    End Class

End Namespace
