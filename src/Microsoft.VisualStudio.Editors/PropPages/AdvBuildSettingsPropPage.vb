' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Globalization
Imports System.Reflection
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.Editors.Common
Imports VSLangProj80

Imports VBStrings = Microsoft.VisualBasic.Strings

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Friend Class AdvBuildSettingsPropPage
        Inherits PropPageUserControlBase

        Protected DebugSymbols As Boolean

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            cboReportCompilerErrors.Items.AddRange(New Object() {New ComboItem("none", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_ReportCompilerErrors_None), New ComboItem("prompt", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_ReportCompilerErrors_Prompt), New ComboItem("send", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_ReportCompilerErrors_Send), New ComboItem("queue", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_ReportCompilerErrors_Queue)})
            cboDebugInfo.Items.AddRange(New Object() {New ComboItem("none", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_DebugInfo_None), New ComboItem("full", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_DebugInfo_Full), New ComboItem("pdbonly", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_DebugInfo_PdbOnly), New ComboItem("portable", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_DebugInfo_Portable), New ComboItem("embedded", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_DebugInfo_Embedded)})

            PopulateLanguageVersions()
            SetupLangVersionLinkLabel()

            ' Scale the width of the overarching table layout panel
            overarchingTableLayoutPanel.Width = DpiAwareness.LogicalToDeviceUnits(Handle, overarchingTableLayoutPanel.Width)

            MinimumSize = PreferredSize()

            AddChangeHandlers()

            AutoScaleMode = AutoScaleMode.Font
            PageRequiresScaling = False

            TelemetryLogger.LogAdvBuildSettingsPropPageEvent(TelemetryLogger.AdvBuildSettingsPropPageEvent.FormOpened)
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(CSharpProjPropId.CSPROJPROPID_ErrorReport, "ErrorReport", cboReportCompilerErrors, AddressOf ErrorReportSet, AddressOf ErrorReportGet, ControlDataFlags.None, New Control() {lblReportCompilerErrors}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_CheckForOverflowUnderflow, "CheckForOverflowUnderflow", chkOverflow, AddressOf OverflowUnderflowSet, AddressOf OverflowUnderflowGet),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_FileAlignment, "FileAlignment", cboFileAlignment, AddressOf FileAlignmentSet, AddressOf FileAlignmentGet, ControlDataFlags.None, New Control() {lblFileAlignment}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_BaseAddress, "BaseAddress", txtDLLBase, AddressOf BaseAddressSet, AddressOf BaseAddressGet, ControlDataFlags.None, New Control() {lblDLLBase}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_DebugInfo, "DebugInfo", cboDebugInfo, AddressOf DebugInfoSet, AddressOf DebugInfoGet, ControlDataFlags.None, New Control() {lblDebugInfo}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DebugSymbols, "DebugSymbols", Nothing, AddressOf DebugSymbolsSet, AddressOf DebugSymbolsGet)}
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' Populates the cboLanguageVersion 
        ''' 829715
        ''' </summary>
        Private Sub PopulateLanguageVersions()
            cboLanguageVersion.Items.Add(My.Resources.Strings.LanguageVersionAutomaticallySelected)
            cboLanguageVersion.SelectedIndex = 0
        End Sub

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            MyBase.PreInitPage()
        End Sub

        ''' <summary>
        ''' Sets up the link to take the user to more information on why they can't change the language
        ''' version.
        ''' </summary>
        Private Sub SetupLangVersionLinkLabel()
            Dim link = New LinkLabel.Link With {
                .Length = lnkLabel.Text.Length,
                .LinkData = My.Resources.Strings.CantSelectLanguageVersionFWLink
            }
            lnkLabel.Links.Add(link)
            lnkLabel.TabStop = True
        End Sub

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
        End Sub

        Private Function ErrorReportSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    SelectComboItem(cboReportCompilerErrors, stValue)
                Else
                    cboReportCompilerErrors.SelectedIndex = 0        ' Zero is the (none) entry in the list
                End If
                Return True
            Else
                cboReportCompilerErrors.SelectedIndex = -1        ' Indeterminate state
            End If
        End Function

        Private Function ErrorReportGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim item As ComboItem = CType(CType(control, ComboBox).SelectedItem, ComboItem)
            If item IsNot Nothing Then
                value = item.Value
                Return True
            Else
                Return False         ' Indeterminate - let the architecture handle it
            End If
        End Function

        Private Function DebugSymbolsSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate/IsMissing 
                DebugSymbols = False
            Else
                DebugSymbols = CType(value, Boolean)
            End If
            Return True
        End Function

        Private Function DebugSymbolsGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = DebugSymbols
            Return True
        End Function

        Private Function BaseAddressSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If IsExeProject() Then
                ' EXE's don't support base addresses so just disable the control and set the text to the default for 
                ' EXE's.

                txtDLLBase.Enabled = False
                txtDLLBase.Text = "0x00400000"
            Else
                ' The default for DLL projects is 0x11000000
                txtDLLBase.Enabled = True

                Dim iBaseAddress As UInteger

                Dim throwAwayObject As Decimal = Nothing
                If TypeOf value Is UInteger OrElse
                    (CpsPropertyDescriptorWrapper.IsAnyCpsComponent(m_Objects) AndAlso Decimal.TryParse(DirectCast(value, String), throwAwayObject)) Then
                    iBaseAddress = CUInt(value)
                Else
                    ' Since it's bogus just use the default for DLLs
                    iBaseAddress = &H11000000   ' 0x11000000
                End If

                Dim stHexValue As String = "0x" & iBaseAddress.ToString("x", CultureInfo.CurrentUICulture)
                If value Is PropertyControlData.Indeterminate Then
                    stHexValue = ""
                End If
                txtDLLBase.Text = stHexValue
            End If

            Return True
        End Function

        Private Function IsExeProject() As Boolean

            Dim obj As Object = Nothing
            Dim OutputType As VSLangProj.prjOutputType

            Try
                GetCurrentProperty(VsProjPropId.VBPROJPROPID_OutputType, "OutputType", obj)
                OutputType = CType(obj, VSLangProj.prjOutputType)
            Catch ex As InvalidCastException
                ' When all else fails assume dll (so they can edit it)
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            Catch ex As TargetInvocationException
                ' Property must be missing for this project flavor
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            End Try

            If OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                Return False
            Else
                Return True
            End If
        End Function

        Private Function BaseAddressGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim StringValue As String = Trim(control.Text)

            'DLL Baseaddress must be 0xNNNNNNNN format
            If String.Equals(VBStrings.Left(StringValue, 2), "0x", StringComparison.OrdinalIgnoreCase) Then
                StringValue = "&h" + Mid(StringValue, 3)
                If IsNumeric(StringValue) Then
                    Dim LongValue As ULong
                    Try
                        LongValue = CULng(StringValue)
                        If LongValue < UInteger.MaxValue Then
                            value = CUInt(LongValue)
                            Return True
                        End If
                    Catch ex As Exception
                        'Let throw below
                    End Try
                End If
            End If
            Throw New Exception(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_InvalidBaseAddress)

        End Function

        Private Function DebugInfoSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                cboDebugInfo.SelectedIndex = -1
            Else
                Dim stValue As String = TryCast(value, String)
                If (stValue IsNot Nothing) AndAlso (stValue.Trim().Length > 0) Then
                    SelectComboItem(cboDebugInfo, stValue)
                Else
                    cboDebugInfo.SelectedIndex = 0        ' Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        Private Function DebugInfoGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim item As ComboItem = CType(CType(control, ComboBox).SelectedItem, ComboItem)

            If item IsNot Nothing Then
                value = item.Value
            Else
                value = "none"
            End If

            Return True
        End Function

        Private Sub DebugInfo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboDebugInfo.SelectedIndexChanged
            If cboDebugInfo.SelectedIndex = 0 Then
                ' user selected none
                DebugSymbols = False
            Else
                DebugSymbols = True
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_DebugSymbols, False)
            SetDirty(VsProjPropId80.VBPROJPROPID_DebugInfo, False)
            SetDirty(True)
        End Sub

        Private Function FileAlignmentSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                cboFileAlignment.SelectedIndex = -1
            Else
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    cboFileAlignment.Text = stValue
                Else
                    cboFileAlignment.SelectedIndex = 0        ' Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        Private Function FileAlignmentGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = cboFileAlignment.Text
            Return True
        End Function

        Private Function OverflowUnderflowSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate Then
                chkOverflow.CheckState = CheckState.Indeterminate
            Else
                chkOverflow.Checked = CType(value, Boolean)
            End If
            Return True
        End Function

        Private Function OverflowUnderflowGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = chkOverflow.Checked
            Return True
        End Function

        Private Shared Sub SelectComboItem(control As ComboBox, value As String)
            For Each entry As ComboItem In control.Items
                If entry.Value = value Then
                    control.SelectedItem = entry
                    Exit For
                End If
            Next
        End Sub

        Private Sub LnkLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkLabel.LinkClicked
            TelemetryLogger.LogAdvBuildSettingsPropPageEvent(TelemetryLogger.AdvBuildSettingsPropPageEvent.LangVersionMoreInfoLinkClicked)

            Dim url = DirectCast(e.Link.LinkData, String)
            Try
                Process.Start(url)
            Catch ex As Exception
                ' This could throw an exception if the user has no default URL handler installed.
                ' There's no point in letting such an exception propagate.
            End Try
        End Sub
    End Class

End Namespace
