' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A customized property control data for the target framework combo box
    ''' </summary>
    Friend Class TargetFrameworkPropertyControlData
        Inherits PropertyControlData

        Private ReadOnly _comboBox As ComboBox

        Public Sub New(id As Integer, comboBox As ComboBox, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags, assocControls As Control())
            'Setting name as empty string, as we will later bind it.
            MyBase.New(id, String.Empty, comboBox, setter, getter, flags, assocControls)
            _comboBox = comboBox

            AddHandler _comboBox.DropDownClosed, AddressOf ComboBox_DropDownClosed
        End Sub

        Public Overrides Sub Initialize(propertyPage As PropPageUserControlBase)
            'We dynamically bind the property name depending if the project is legacy or not.
            If propertyPage.ProjectHierarchy.IsCapabilityMatch("CPS") Then
                PropertyName = ApplicationPropPage.Const_TargetFramework
            Else
                PropertyName = ApplicationPropPage.Const_TargetFrameworkMoniker
            End If
            MyBase.Initialize(propertyPage)
        End Sub

        Private Function IsInstallOtherFrameworksSelected() As Boolean
            Return _comboBox.SelectedIndex >= 0 AndAlso
                   TypeOf _comboBox.Items(_comboBox.SelectedIndex) Is InstallOtherFrameworksComboBoxValue
        End Function

        Private Sub NavigateToInstallOtherFrameworksFWLink()

            If Site Is Nothing Then
                ' Can't do anything without a site
                Debug.Fail("Why is there no site?")
                Return
            End If

            Using serviceProvider As New ServiceProvider(Site)

                Dim vsUIShellOpenDocument As IVsUIShellOpenDocument = TryCast(serviceProvider.GetService(GetType(SVsUIShellOpenDocument).GUID), IVsUIShellOpenDocument)

                If vsUIShellOpenDocument Is Nothing Then
                    ' Can't do anything without a IVsUIShellOpenDocument
                    Debug.Fail("Why is there no IVsUIShellOpenDocument service?")
                    Return
                End If

                Dim flags As UInteger = 0
                Dim url As String = My.Resources.Strings.InstallOtherFrameworksFWLink
                Dim resolution As VSPREVIEWRESOLUTION = VSPREVIEWRESOLUTION.PR_Default
                Dim reserved As UInteger = 0

                If ErrorHandler.Failed(vsUIShellOpenDocument.OpenStandardPreviewer(flags, url, resolution, reserved)) Then
                    ' Behavior for OpenStandardPreviewer with no flags is to show a message box if
                    ' it fails (will always return S_OK)
                    Debug.Fail("IVsUIShellOpenDocument.OpenStandardPreviewer failed!")
                End If

            End Using

        End Sub

        Private Sub ComboBox_DropDownClosed(sender As Object, e As EventArgs)

            If IsInstallOtherFrameworksSelected() Then

                ' If the drop down is closed and the selection is still on the 'Install other frameworks...' value,
                ' move the selection back to the last target framework value.  This can happen if arrowing when the drop
                ' down is open (no commit) and pressing escape
                _comboBox.SelectedIndex = IndexOfLastCommittedValue

            End If

        End Sub

        Protected Overrides Sub ComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs)

            If IsInstallOtherFrameworksSelected() Then

                ' If the user chooses 'Install other frameworks...', move the selection back to the last target
                ' framework value and navigate to the fwlink
                _comboBox.SelectedIndex = IndexOfLastCommittedValue
                NavigateToInstallOtherFrameworksFWLink()

            ElseIf _comboBox.SelectedIndex <> IndexOfLastCommittedValue Then

                MyBase.ComboBox_SelectionChangeCommitted(sender, e)

                ' Keep track of what the user chose in case 'Install other frameworks...' is chosen later,
                ' which allows us to revert back to this value
                IndexOfLastCommittedValue = _comboBox.SelectedIndex

            End If

        End Sub

        ''' <summary>
        ''' Remove references to objects to prevent memory leaks
        ''' </summary>
        Public Sub Cleanup()

            ' Clear the reference to the COM service provider
            Site = Nothing

            ' Clear the handler added in the constructor
            RemoveHandler _comboBox.DropDownClosed, AddressOf ComboBox_DropDownClosed
        End Sub

        ''' <summary>
        ''' Holds the site provided the parent page when the parent page is able to obtain it
        ''' </summary>
        Public Property Site As IServiceProvider

        ''' <summary>
        ''' Holds the last committed property value.  This can change with user interaction in the combo box
        ''' or by programmatically setting the property value (i.e. DTE)
        ''' </summary>
        Public Property IndexOfLastCommittedValue As Integer = -1

    End Class

End Namespace
