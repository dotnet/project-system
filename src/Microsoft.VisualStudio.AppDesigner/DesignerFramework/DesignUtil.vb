' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

    '**************************************************************************
    ';DesignUtil
    '
    'Remarks:
    '   This class contains utility methods used in the DesignerFramework.
    '   This class is converted from <DD>\wizard\vsdesigner\designer\microsoft\vsdesigner\data\DesignUtil.cs
    '**************************************************************************
    Friend NotInheritable Class DesignUtil

        Private Sub New()
        End Sub

        '**************************************************************************
        ';ReportError
        '
        'Summary:
        '   Displays an error message box with the specified error message.
        'Params:
        '   ServiceProvider: The IServiceProvider, used to get devenv shell as the parent of the message box.
        '   ErrorMessage: The text to display in the message box.
        '**************************************************************************
        Public Overloads Shared Sub ReportError(ServiceProvider As IServiceProvider, ErrorMessage As String)
            ReportError(ServiceProvider, ErrorMessage, Nothing)
        End Sub 'ReportError

        '**************************************************************************
        ';ReportError
        '
        'Summary:
        '   Displays an error message box with the specified error message and help link.
        'Params:
        '   ServiceProvider: The IServiceProvider, used to get devenv shell as the parent of the message box.
        '   ErrorMessage: The text to display in the message box.
        '   HelpLink: Link to the help topic for this message box.
        '**************************************************************************
        Public Overloads Shared Sub ReportError(ServiceProvider As IServiceProvider, ErrorMessage As String,
                HelpLink As String)

            DesignerMessageBox.Show(ServiceProvider, ErrorMessage, GetDefaultCaption(ServiceProvider),
                    MessageBoxButtons.OK, MessageBoxIcon.Error, HelpLink:=HelpLink)
        End Sub 'ReportError

        ''' <summary>
        ''' Get the default caption from IVsUIShell, or fall back to localized resource
        ''' </summary>
        ''' <param name="sp"></param>
        Public Shared Function GetDefaultCaption(sp As IServiceProvider) As String
            Dim caption As String = ""
            Dim uiShell As IVsUIShell = Nothing
            If sp IsNot Nothing Then
                uiShell = DirectCast(sp.GetService(GetType(IVsUIShell)), IVsUIShell)
            End If

            If uiShell Is Nothing OrElse AppDesInterop.NativeMethods.Failed(uiShell.GetAppName(caption)) Then
                caption = My.Resources.Designer.DFX_Error_Default_Caption
            End If

            Return caption
        End Function

        ''' <summary>
        ''' Show help (if at all possible) swallowing any COM exceptions
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="keyword"></param>
        Public Shared Sub DisplayTopicFromF1Keyword(ServiceProvider As IServiceProvider, keyword As String)
            If ServiceProvider Is Nothing Then
                Debug.Fail("NULL serviceprovider - can't show help!")
                Return
            End If

            If keyword Is Nothing Then
                Debug.Fail("NULL help keyword - can't show help!")
                Return
            End If

            Dim vshelp As VSHelp.Help = CType(ServiceProvider.GetService(GetType(VSHelp.Help)), VSHelp.Help)

            If vshelp Is Nothing Then
                Debug.Fail("Failed to get VSHelp.Help service from given service provider - can't show help!")
                Return
            End If

            Try
                vshelp.DisplayTopicFromF1Keyword(keyword)
            Catch ex As COMException
                ' DisplayTopicFromF1Keyword may throw COM exceptions even though dexplore shows the appropriate error message
                Debug.Assert(Marshal.GetHRForException(ex) = &H80040305, String.Format("Unknown COM Exception {0} when trying to show help topic {1}", ex, keyword))
            End Try
        End Sub

        '**************************************************************************
        ';SetFontStyles
        '
        'Summary:
        '   Iterate all controls on a form and make them recreate their fonts with the desired font style.
        'Params:
        '   TopControl: The top-level control containing other controls.
        'Remarks:
        '   This method should be called especially in the OnFontChanged handler. 
        '   This way, when the VS shell font is given to us (it changes) then controls that have a different style 
        '       of the font (bolded for example) will recreate their font and use the VS shell font but bolded.
        '**************************************************************************
        Public Overloads Shared Sub SetFontStyles(TopControl As Control)
            SetFontStyles(TopControl, TopControl, TopControl.Font)
        End Sub 'SetFontStyles

        '= PRIVATE ============================================================

        '**************************************************************************
        ';SetFontStyles
        '
        'Summary:
        '   Recursive method to set the fonts of all the controls on a form.
        'Params:
        '   TopControl: The top-level control containing other controls. Each child control 
        '       will compare their fonts with this control to know whether their styles are different.
        '   Parent: The parent control used to iterate through all the control.
        '   ReferenceFont: The font to set to.
        '**************************************************************************
        Private Overloads Shared Sub SetFontStyles(TopControl As Control, Parent As Control, ReferenceFont As Font)
            For Each ChildControl As Control In Parent.Controls
                If ChildControl.Controls IsNot Nothing AndAlso ChildControl.Controls.Count > 0 Then
                    SetFontStyles(TopControl, ChildControl, ReferenceFont)
                End If

                If Not ChildControl.Font.Equals(TopControl.Font) Then
                    ChildControl.Font = New Font(ReferenceFont, ChildControl.Font.Style)
                End If
            Next ChildControl
        End Sub 'SetFontStyles

    End Class 'DesignUtil

End Namespace

