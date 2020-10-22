' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

'This is the C# version of the Compile property page.  'CompilePropPage2.vb is the VB version.

Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop

Imports VSLangProj110

Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend NotInheritable Class BuildPropPage
        Inherits BuildPropPageBase

        Protected DocumentationFile() As String
        'True when we're changing control values ourselves
        Protected InsideInternalUpdate As Boolean

        ' Stored conditional compilation symbols. We need these to calculate the new strings
        '   to return for the conditional compilation constants when the user changes any
        '   of the controls related to conditional compilation symbols (the data in the
        '   controls is not sufficient because they could be indeterminate, and we are acting
        '   as if we have three separate properties, so we need the original property values).
        ' Array same length and indexing as the objects passed in to SetObjects.
        Protected CondCompSymbols() As String
        Protected Const Const_DebugConfiguration As String = "Debug" 'Name of the debug configuration
        Protected Const Const_ReleaseConfiguration As String = "Release" 'Name of the release configuration
        Protected Const Const_CondConstantDEBUG As String = "DEBUG"
        Protected Const Const_CondConstantTRACE As String = "TRACE"

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            cboNullable.Items.AddRange(New Object() {
                New ComboItem("disable", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildSettings_Nullable_Disable),
                New ComboItem("enable", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildSettings_Nullable_Enable),
                New ComboItem("warnings", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildSettings_Nullable_Warnings),
                New ComboItem("annotations", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildSettings_Nullable_Annotations)})
        End Sub

        Public Enum TreatWarningsSetting
            WARNINGS_ALL
            WARNINGS_SPECIFIC
            WARNINGS_NONE
        End Enum

        Protected Overrides Sub EnableAllControls(enabled As Boolean)
            MyBase.EnableAllControls(enabled)
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants, "DefineConstants", txtConditionalCompilationSymbols, AddressOf ConditionalCompilationSet, AddressOf ConditionalCompilationGet, ControlDataFlags.None, New Control() {txtConditionalCompilationSymbols, chkDefineDebug, chkDefineTrace, lblConditionalCompilationSymbols}),
                     New PropertyControlData(VsProjPropId80.VBPROJPROPID_PlatformTarget, "PlatformTarget", cboPlatformTarget, AddressOf PlatformTargetSet, AddressOf PlatformTargetGet, ControlDataFlags.None, New Control() {lblPlatformTarget}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_AllowUnsafeBlocks, "AllowUnsafeBlocks", chkAllowUnsafeCode),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_Optimize, "Optimize", chkOptimizeCode),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_WarningLevel, "WarningLevel", cboWarningLevel, AddressOf WarningLevelSet, AddressOf WarningLevelGet, ControlDataFlags.None, New Control() {lblWarningLevel}),
                     New PropertyControlData(VsProjPropId2.VBPROJPROPID_NoWarn, "NoWarn", txtSupressWarnings, New Control() {lblSupressWarnings}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors, "TreatWarningsAsErrors", rbWarningAll, AddressOf TreatWarningsInit, AddressOf TreatWarningsGet),
                     New PropertyControlData(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, "TreatSpecificWarningsAsErrors", txtSpecificWarnings, AddressOf TreatSpecificWarningsInit, AddressOf TreatSpecificWarningsGet),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_OutputPath, "OutputPath", txtOutputPath, New Control() {lblOutputPath}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile, "DocumentationFile", txtXMLDocumentationFile, AddressOf XMLDocumentationFileInit, AddressOf XMLDocumentationFileGet, ControlDataFlags.None, New Control() {txtXMLDocumentationFile, chkXMLDocumentationFile}),
                     New PropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop, "RegisterForComInterop", chkRegisterForCOM, AddressOf RegisterForCOMInteropSet, AddressOf RegisterForCOMInteropGet),
                     New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, "OutputTypeEx", Nothing, AddressOf OutputTypeSet, Nothing),
                     New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_GenerateSerializationAssemblies, "GenerateSerializationAssemblies", cboSGenOption, New Control() {lblSGenOption}),
                     New PropertyControlData(VsProjPropId110.VBPROJPROPID_Prefer32Bit, "Prefer32Bit", chkPrefer32Bit, AddressOf Prefer32BitSet, AddressOf Prefer32BitGet),
                     New HiddenIfMissingPropertyControlData(1, "Nullable", cboNullable, AddressOf NullableSet, AddressOf NullableGet, ControlDataFlags.None, New Control() {lblNullable}),
                     New PropertyControlData(CSharpProjPropId.CSPROJPROPID_LanguageVersion, "LanguageVersion", Nothing, AddressOf LanguageVersionSet, Nothing, ControlDataFlags.None, Nothing)
                     }
                End If
                Return m_ControlData
            End Get
        End Property

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

            cboPlatformTarget.Items.Clear()

            Dim PlatformEntries As New List(Of String)

            ' Let's try to sniff the supported platforms from our hierarchy (if any)
            If ProjectHierarchy IsNot Nothing Then
                Dim oCfgProv As Object = Nothing
                Dim hr As Integer
                hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, oCfgProv)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim cfgProv As IVsCfgProvider2 = TryCast(oCfgProv, IVsCfgProvider2)
                    If cfgProv IsNot Nothing Then
                        Dim actualPlatformCount(0) As UInteger
                        hr = cfgProv.GetSupportedPlatformNames(0, Nothing, actualPlatformCount)
                        If VSErrorHandler.Succeeded(hr) Then
                            Dim platformCount As UInteger = actualPlatformCount(0)
                            Dim platforms(CInt(platformCount)) As String
                            hr = cfgProv.GetSupportedPlatformNames(platformCount, platforms, actualPlatformCount)
                            If VSErrorHandler.Succeeded(hr) Then
                                For platformNo As Integer = 0 To CInt(platformCount - 1)
                                    PlatformEntries.Add(platforms(platformNo))
                                Next
                            End If
                        End If
                    End If
                End If
            End If

            ' ...and if we couldn't get 'em from the project system, let's add a hard-coded list of platforms...
            If PlatformEntries.Count = 0 Then
                Debug.Fail("Unable to get platform list from configuration manager")
                PlatformEntries.AddRange(New String() {"Any CPU", "x86", "x64", "Itanium"})
            End If
            If VSProductSKU.ProductSKU < VSProductSKU.VSASKUEdition.Enterprise Then
                'For everything lower than VSTS (SKU# = Enterprise), don't target Itanium
                PlatformEntries.Remove("Itanium")
            End If

            ' ... Finally, add the entries to the combobox
            cboPlatformTarget.Items.AddRange(PlatformEntries.ToArray())
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

            'OutputPath browse button should only be enabled when the text box is enabled and Not ReadOnly
            btnOutputPathBrowse.Enabled = txtOutputPath.Enabled AndAlso Not txtOutputPath.ReadOnly

            rbWarningNone.Enabled = rbWarningAll.Enabled
            rbWarningSpecific.Enabled = rbWarningAll.Enabled

            RefreshEnabledStatusForPrefer32Bit(chkPrefer32Bit)
            RefreshVisibleStatusForNullable()
        End Sub

        Private Sub AdvancedButton_Click(sender As Object, e As EventArgs) Handles btnAdvanced.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedBuildSettings_Title, GetType(AdvBuildSettingsPropPage), HelpKeywords.CSProjPropAdvancedCompile)
        End Sub

        Private Function NullableSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                Dim stValue As String = CType(value, String)
                If Not String.IsNullOrEmpty(stValue) Then
                    SelectComboItem(cboNullable, stValue)
                Else
                    cboNullable.SelectedIndex = 0 ' Zero is the (disabled) entry in the list
                End If
                Return True
            Else
                cboNullable.SelectedIndex = -1 ' Indeterminate state
            End If
        End Function

        Private Function NullableGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim item As ComboItem = CType(CType(control, ComboBox).SelectedItem, ComboItem)
            If item IsNot Nothing Then
                value = item.Value
                Return True
            Else
                Return False ' Indeterminate
            End If
        End Function

        Private Shared Sub SelectComboItem(control As ComboBox, value As String)
            For Each entry As ComboItem In control.Items
                If entry.Value = value Then
                    control.SelectedItem = entry
                    Exit For
                End If
            Next
        End Sub

        Private Sub RefreshVisibleStatusForNullable()
            Dim pcd = GetPropertyControlData("LanguageVersion")
            Dim value = TryCast(pcd.GetPropertyValueNative(pcd.ExtendedPropertiesObjects), String)

            ' If the project doesn't specify a LangVersion property, one is determined based upon TargetFramework.
            ' For new target frameworks such as netcoreapp3.1 that support C# 8 and Nullable Reference Types, the
            ' LangVersion property is not set. For target frameworks which do not support C# 8 by default (such as
            ' net472) LangVersion is set to a string such as "7.3"
            Dim version As Decimal
            Dim supportNullable = String.IsNullOrEmpty(value) OrElse (Decimal.TryParse(value, version) AndAlso version >= 8D)

            EnableControl(lblNullable, supportNullable)
            EnableControl(cboNullable, supportNullable)
        End Sub

        Private Function LanguageVersionSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not m_fInsideInit AndAlso Not InsideInternalUpdate Then
                ' Changes to the LanguageVersion may affect whether nullable is enabled
                RefreshVisibleStatusForNullable()
            End If

            Return True
        End Function

        Private Function ShouldEnableRegisterForCOM() As Boolean

            Dim obj As Object = Nothing
            Dim outputType As UInteger

            Try
                If GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, obj) Then
                    outputType = CUInt(obj)
                Else
                    Return True
                End If
            Catch exc As InvalidCastException
                Return True
            Catch exc As NullReferenceException
                Return True
            Catch ex As TargetInvocationException
                Return True
            End Try

            ' Only supported for libraries
            Return outputType = prjOutputTypeEx.prjOutputTypeEx_Library

        End Function

        Private Function OutputTypeSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not ShouldEnableRegisterForCOM() Then
                chkRegisterForCOM.Enabled = False
            Else
                EnableControl(chkRegisterForCOM, True)
            End If

            If Not m_fInsideInit AndAlso Not InsideInternalUpdate Then
                ' Changes to the OutputType may affect whether Prefer32Bit is enabled
                RefreshEnabledStatusForPrefer32Bit(chkPrefer32Bit)
            End If

            Return True
        End Function

        Private Function RegisterForCOMInteropSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                Dim bRegisterForCOM As Boolean = False
                Dim propRegisterForCOM As PropertyDescriptor
                Dim obj As Object

                propRegisterForCOM = GetPropertyDescriptor("RegisterForComInterop")
                obj = TryGetNonCommonPropertyValue(propRegisterForCOM)

                If obj IsNot PropertyControlData.MissingProperty Then
                    If obj IsNot PropertyControlData.Indeterminate Then
                        bRegisterForCOM = CType(obj, String) IsNot "" AndAlso CType(obj, Boolean)
                    End If

                    chkRegisterForCOM.Checked = bRegisterForCOM

                    ' Checkbox is only enabled for DLL projects
                    If Not ShouldEnableRegisterForCOM() Then
                        chkRegisterForCOM.Enabled = False
                    Else
                        EnableControl(chkRegisterForCOM, True)
                    End If

                    Return True
                Else
                    chkRegisterForCOM.Enabled = False
                    chkRegisterForCOM.CheckState = CheckState.Indeterminate
                    Return True
                End If
            Else
                chkRegisterForCOM.CheckState = CheckState.Indeterminate
                Return True
            End If
        End Function

        Private Function RegisterForCOMInteropGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If chkRegisterForCOM.CheckState <> CheckState.Indeterminate Then
                value = chkRegisterForCOM.Checked
                Return True
            Else
                Return False   ' Let the framework handle it since its indeterminate
            End If
        End Function

        Private Sub OutputPathBrowse_Click(sender As Object, e As EventArgs) Handles btnOutputPathBrowse.Click
            Dim DirName As String = Nothing
            If GetDirectoryViaBrowseRelativeToProject(txtOutputPath.Text, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_SelectOutputPathTitle, DirName) Then
                txtOutputPath.Text = DirName
                SetDirty(True) ' vswhidbey 276000 - textchanged events do not commit, lostfocus does
                ' this code path should commit the change if the user selected a new outputpath via the picker
            Else
                'User cancelled out of dialog
            End If
        End Sub

        Private Function TreatSpecificWarningsInit(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            InsideInternalUpdate = True

            Try
                Dim bIndeterminateState As Boolean = False
                Dim warnings As TreatWarningsSetting

                If Not PropertyControlData.IsSpecialValue(value) Then
                    Dim stSpecificWarnings As String

                    stSpecificWarnings = CType(value, String)
                    If stSpecificWarnings <> "" Then
                        warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                        txtSpecificWarnings.Text = stSpecificWarnings

                        bIndeterminateState = False
                    Else
                        Dim propTreatAllWarnings As PropertyDescriptor
                        Dim obj As Object

                        propTreatAllWarnings = GetPropertyDescriptor("TreatWarningsAsErrors")

                        obj = TryGetNonCommonPropertyValue(propTreatAllWarnings)

                        If Not PropertyControlData.IsSpecialValue(obj) Then
                            txtSpecificWarnings.Text = ""
                            Dim bTreatAllWarningsAsErrors = CType(obj, Boolean)
                            If bTreatAllWarningsAsErrors Then
                                warnings = TreatWarningsSetting.WARNINGS_ALL
                            Else
                                warnings = TreatWarningsSetting.WARNINGS_NONE
                            End If

                            bIndeterminateState = False
                        Else
                            ' Since TreadAllWarnings is indeterminate we should be too
                            bIndeterminateState = True
                        End If
                    End If
                Else
                    ' Indeterminate. Leave all the radio buttons unchecked
                    bIndeterminateState = True
                End If

                If Not bIndeterminateState Then
                    rbWarningAll.Checked = warnings = TreatWarningsSetting.WARNINGS_ALL
                    rbWarningSpecific.Checked = warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                    txtSpecificWarnings.Enabled = warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                    rbWarningNone.Checked = warnings = TreatWarningsSetting.WARNINGS_NONE
                Else
                    rbWarningAll.Checked = False
                    rbWarningSpecific.Checked = False
                    txtSpecificWarnings.Enabled = False
                    txtSpecificWarnings.Text = ""
                    rbWarningNone.Checked = False
                End If
            Finally
                InsideInternalUpdate = False
            End Try

            Return True
        End Function

        Private Function TreatSpecificWarningsGetValue() As TreatWarningsSetting
            Dim warnings As TreatWarningsSetting

            If rbWarningAll.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_ALL
            ElseIf rbWarningSpecific.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
            ElseIf rbWarningNone.Checked Then
                warnings = TreatWarningsSetting.WARNINGS_NONE
            Else
                warnings = TreatWarningsSetting.WARNINGS_NONE
            End If

            Return warnings
        End Function

        Private Function TreatSpecificWarningsGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim bRetVal As Boolean

            If rbWarningAll.Checked Then
                value = ""
                bRetVal = True
            ElseIf rbWarningSpecific.Checked Then
                value = txtSpecificWarnings.Text
                bRetVal = True
            ElseIf rbWarningNone.Checked Then
                value = ""
                bRetVal = True
            Else
                ' We're in the indeterminate state. Let the architecture handle it
                bRetVal = False
            End If

            Return bRetVal
        End Function

        Private Function TreatWarningsInit(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            ' Don't need to do anything here (it's done in TreatSpecificWarningsInit)
            Return True
        End Function

        Private Function TreatWarningsGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim bRetVal As Boolean

            If rbWarningAll.Checked Then
                value = rbWarningAll.Checked
                bRetVal = True
            ElseIf rbWarningSpecific.Checked Then
                value = False
                bRetVal = True
            ElseIf rbWarningNone.Checked Then
                value = Not rbWarningNone.Checked    ' If none is checked we want value to be false
                bRetVal = True
            Else
                ' We're in the indeterminate state. Let the architecture handle it.
                bRetVal = False
            End If

            Return bRetVal
        End Function

        Private Sub rbStartAction_CheckedChanged(sender As Object, e As EventArgs) Handles rbWarningAll.CheckedChanged, rbWarningSpecific.CheckedChanged, rbWarningNone.CheckedChanged
            If Not InsideInternalUpdate Then
                Dim warnings As TreatWarningsSetting = TreatSpecificWarningsGetValue()
                rbWarningAll.Checked = warnings = TreatWarningsSetting.WARNINGS_ALL
                rbWarningSpecific.Checked = warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                txtSpecificWarnings.Enabled = warnings = TreatWarningsSetting.WARNINGS_SPECIFIC
                rbWarningNone.Checked = warnings = TreatWarningsSetting.WARNINGS_NONE
                IsDirty = True

                ' Dirty both of the properties since either one could have changed
                SetDirty(rbWarningAll)
                SetDirty(txtSpecificWarnings)
            End If
        End Sub

        Private Function XMLDocumentationFileInit(control As Control, prop As PropertyDescriptor, values() As Object) As Boolean
            Dim bOriginalState As Boolean = InsideInternalUpdate

            InsideInternalUpdate = True
            ReDim DocumentationFile(values.Length - 1)
            values.CopyTo(DocumentationFile, 0)

            Dim objDocumentationFile As Object
            objDocumentationFile = PropertyControlData.GetValueOrIndeterminateFromArray(DocumentationFile)

            If Not PropertyControlData.IsSpecialValue(objDocumentationFile) Then
                If Trim(TryCast(objDocumentationFile, String)) <> "" Then
                    txtXMLDocumentationFile.Text = Trim(TryCast(objDocumentationFile, String))
                    chkXMLDocumentationFile.Checked = True
                    txtXMLDocumentationFile.Enabled = True
                Else
                    chkXMLDocumentationFile.Checked = False
                    txtXMLDocumentationFile.Enabled = False
                    txtXMLDocumentationFile.Text = ""
                End If
            Else
                chkXMLDocumentationFile.CheckState = CheckState.Indeterminate
                txtXMLDocumentationFile.Text = ""
                txtXMLDocumentationFile.Enabled = False
            End If

            ' Reset value
            InsideInternalUpdate = bOriginalState
            Return True
        End Function

        Private Function XMLDocumentationFileGet(control As Control, prop As PropertyDescriptor, ByRef values() As Object) As Boolean
            Debug.Assert(DocumentationFile IsNot Nothing)
            ReDim values(DocumentationFile.Length - 1)
            DocumentationFile.CopyTo(values, 0)
            Return True
        End Function

        Protected Overrides Function GetF1HelpKeyword() As String
            Debug.Assert(IsCSProject, "Unknown project type")
            Return HelpKeywords.CSProjPropBuild
        End Function

        Private Function WarningLevelSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                Dim warningLevel = CType(value, Integer)
                Dim indexAsString = warningLevel.ToString()
                ' Lookup the index of the given warning level in the combobox
                Dim indexLocation = If(warningLevel = 9999, GetIndexLocation(My.Resources.Strings.preview), GetIndexLocation(indexAsString))

                If indexLocation <> -1 Then
                    ' If there is an existing entry use that
                    cboWarningLevel.SelectedIndex = indexLocation
                ElseIf warningLevel = 9999 Then
                    ' Otherwise add a new entry
                    ' 9999 is a special value meaning use the preview warning level
                    cboWarningLevel.Items.Add(My.Resources.Strings.preview)
                    cboWarningLevel.SelectedIndex = cboWarningLevel.Items.Count - 1
                Else
                    ' any non - negative number can be specified but we only want to show them in the combo box if the value is set
                    cboWarningLevel.Items.Add(indexAsString)
                    cboWarningLevel.SelectedIndex = cboWarningLevel.Items.Count - 1
                End If
                Return True
            Else
                ' Indeterminate. Let the architecture handle
                cboWarningLevel.SelectedIndex = -1
                Return True
            End If
        End Function

        Private Function GetIndexLocation(indexToSearchFor As String) As Integer
            Return cboWarningLevel.Items.Cast(Of String).ToList().FindIndex(Function(s)
                                                                                Return s = indexToSearchFor
                                                                            End Function)
        End Function

        Private Function WarningLevelGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim selectedItem = cboWarningLevel.Items.Cast(Of String).ToList()(cboWarningLevel.SelectedIndex)
            If selectedItem = My.Resources.Strings.preview Then
                value = 9999
            Else
                value = CType(selectedItem, Integer)
            End If
            Return True
        End Function

        Private Function PlatformTargetSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                If IsNothing(TryCast(value, String)) OrElse TryCast(value, String) = "" Then
                    cboPlatformTarget.SelectedIndex = 0     ' AnyCPU
                Else
                    Dim strPlatform As String = TryCast(value, String)

                    ' vswhidbey 474635: For Undo, we may get called to set the value
                    ' to AnyCpu (no space but the one we display in the combobox has a space so
                    ' convert to the one with the space for this specific case

                    ' Convert the no-space to one with a space
                    If String.Equals(strPlatform, "AnyCPU", StringComparison.Ordinal) Then
                        strPlatform = "Any CPU"
                    End If

                    cboPlatformTarget.SelectedItem = strPlatform

                    If cboPlatformTarget.SelectedIndex = -1 Then   ' If we can't find a match
                        If VSProductSKU.IsStandard Then
                            ' For the standard SKU, we do not include Itanium in the list. However,
                            ' if the property is already set to Itanium (most likely from the project file set from
                            ' a non-Standard SKU then add it to the list so we do not report the wrong
                            ' platform target to the user.

                            Dim stValue As String = TryCast(value, String)
                            If String.Equals(Trim(stValue), "Itanium", StringComparison.Ordinal) Then
                                cboPlatformTarget.Items.Add("Itanium")
                                cboPlatformTarget.SelectedItem = stValue
                            Else
                                ' Note that the project system will return "AnyCPU" (no space) but in the UI we want to show the one with a space
                                cboPlatformTarget.SelectedItem = "Any CPU"
                            End If
                        Else
                            ' Note that the project system will return "AnyCPU" (no space) but in the UI we want to show the one with a space
                            cboPlatformTarget.SelectedItem = "Any CPU"
                        End If
                    End If
                End If
                Return True
            Else
                ' Indeterminate - allow the architecture to handle
                Return False
            End If
        End Function

        Private Function PlatformTargetGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            ' SelectedItem may be Nothing if the PlatformTarget property isn't supported
            If cboPlatformTarget.SelectedItem Is Nothing Then
                Return False
            End If

            If (cboPlatformTarget.SelectedItem.ToString() <> "AnyCPU") And (cboPlatformTarget.SelectedItem.ToString() <> "Any CPU") Then
                value = cboPlatformTarget.SelectedItem
            Else
                ' Return to the project system the one without a space
                value = "AnyCPU"
            End If

            Return True
        End Function

        Private Sub XMLDocumentationEnable_CheckStateChanged(sender As Object, e As EventArgs) Handles chkXMLDocumentationFile.CheckStateChanged
            Const XML_FILE_EXTENSION As String = ".xml"

            If chkXMLDocumentationFile.Checked Then

                ' Enable the textbox
                txtXMLDocumentationFile.Enabled = True

                If Trim(txtXMLDocumentationFile.Text) = "" Then
                    ' The textbox is empty so initialize it
                    Dim stOutputPath As String
                    Dim stAssemblyName As String
                    Dim obj As Object = Nothing

                    ' Get OutputPath for all configs. We're going to calculate the documentation file
                    ' for each config (and the value is dependant on the OutputPath

                    Dim RawDocFiles() As Object = RawPropertiesObjects(GetPropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile))
                    Dim OutputPathData() As Object
                    Dim cLen As Integer = RawDocFiles.Length

                    ReDim OutputPathData(cLen)

                    Dim p As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath)
                    For i As Integer = 0 To cLen - 1
                        OutputPathData(i) = p.GetPropertyValueNative(RawDocFiles(i))
                    Next i

                    GetCurrentProperty(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", obj)
                    stAssemblyName = TryCast(obj, String)

                    GetCurrentProperty(VsProjPropId.VBPROJPROPID_AbsoluteProjectDirectory, "AbsoluteProjectDirectory", obj)
                    Dim stProjectDirectory As String = TryCast(obj, String)
                    If VisualBasic.Right(stProjectDirectory, 1) <> "\" Then
                        stProjectDirectory &= "\"
                    End If

                    If Not IsNothing(DocumentationFile) Then
                        ' Loop through each config and calculate what we think the output path should be
                        Dim i As Integer

                        For i = 0 To DocumentationFile.Length - 1

                            If Not IsNothing(OutputPathData) Then
                                stOutputPath = TryCast(OutputPathData(i), String)
                            Else
                                GetProperty(VsProjPropId.VBPROJPROPID_OutputPath, obj)
                                stOutputPath = CType(obj, String)
                            End If

                            If Not IsNothing(stOutputPath) Then
                                If VisualBasic.Right(stOutputPath, 1) <> "\" Then
                                    stOutputPath &= "\"
                                End If

                                If Path.IsPathRooted(stOutputPath) Then
                                    ' stOutputPath is an Absolute path so check to see if its within the project path

                                    If String.Equals(Path.GetFullPath(stProjectDirectory),
                                                      VisualBasic.Left(Path.GetFullPath(stOutputPath), Len(stProjectDirectory)),
                                                      StringComparison.Ordinal) Then

                                        ' The output path is within the project so suggest the output directory (or suggest just the filename
                                        ' which will put it in the default location

                                        DocumentationFile(i) = stOutputPath & stAssemblyName & XML_FILE_EXTENSION

                                    Else

                                        ' The output path is outside the project so just suggest the project directory.
                                        DocumentationFile(i) = stProjectDirectory & stAssemblyName & XML_FILE_EXTENSION

                                    End If

                                Else
                                    ' OutputPath is a Relative path so it will be based on the project directory. use
                                    ' the OutputPath to suggest a location for the documentation file
                                    DocumentationFile(i) = stOutputPath & stAssemblyName & XML_FILE_EXTENSION
                                End If

                            End If
                        Next

                        ' Now if all the values are the same then set the textbox text
                        Dim objDocumentationFile As Object
                        objDocumentationFile = PropertyControlData.GetValueOrIndeterminateFromArray(DocumentationFile)

                        If Not PropertyControlData.IsSpecialValue(objDocumentationFile) Then
                            txtXMLDocumentationFile.Text = TryCast(objDocumentationFile, String)
                        End If
                    End If
                End If

                txtXMLDocumentationFile.Focus()
            Else
                ' Disable the checkbox
                txtXMLDocumentationFile.Enabled = False
                txtXMLDocumentationFile.Text = ""

                ' Clear the values
                Dim i As Integer
                For i = 0 To DocumentationFile.Length - 1
                    DocumentationFile(i) = ""
                Next
            End If

            If Not InsideInternalUpdate Then
                SetDirty(txtXMLDocumentationFile)
            End If
        End Sub

        ''' <summary>
        ''' Fired when the conditional compilations constants textbox has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values
        ''' </summary>
        Private Sub DocumentationFile_TextChanged(sender As Object, e As EventArgs) Handles txtXMLDocumentationFile.TextChanged
            If Not InsideInternalUpdate Then
                Debug.Assert(DocumentationFile IsNot Nothing)
                For i As Integer = 0 To DocumentationFile.Length - 1
                    'store it
                    DocumentationFile(i) = txtXMLDocumentationFile.Text
                Next

                'No need to mark the property dirty - the property page architecture hooks up the FormControl automatically
                '  to TextChanged and will mark it dirty, and will make sure it's persisted on LostFocus.
            End If
        End Sub

        Private Sub PlatformTarget_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles cboPlatformTarget.SelectionChangeCommitted
            If m_fInsideInit OrElse InsideInternalUpdate Then
                Return
            End If

            ' Changes to the PlatformTarget may affect whether Prefer32Bit is enabled
            RefreshEnabledStatusForPrefer32Bit(chkPrefer32Bit)
        End Sub

#Region "Special handling of the conditional compilation constants textbox and the Define DEBUG/TRACE checkboxes"

        'Intended behavior:
        '  For simplified configurations mode ("Tools.Options.Projects and Solutions.Show Advanced Configurations" is off),
        '    we want the display to show only the release value for the DEBUG constant, and keep DEBUG defined always for
        '    the Debug configuration.  If the user changes the DEBUG constant checkbox in simplified mode, then the change
        '    should only affect the Debug configuration.
        '    For the TRACE constant checkbox, we want the normal behavior (show indeterminate if they're different, but they
        '    won't be for the default templates in simplified configs mode).
        '    The conditional compilation textbox likewise should show indeterminate if the debug and release values differ, but
        '    for the default templates they won't.
        '    This behavior is not easy to get, because the DEBUG/TRACE checkboxes are not actual properties in C# like they
        '    are in VB, but are rather parsed from the conditional compilation value.  The conditional compilation textbox
        '    then shows any remaining constants that the user defines besides DEBUG and TRACE>
        '  For advanced configurations, we still parse the conditional compilation constants into DEBUG, TRACE, and everything
        '    else, but we should use normal indeterminate behavior for all of these controls if the values differ in any of the
        '    selected configurations.
        '
        'Note: a minor disadvantage with the current implementation is that the property page architecture doesn't know about
        '  the virtual "DEBUG" and "TRACE" properties that we've created, so the undo/redo descriptions for changes to these
        '  properties will always just say "DefineConstants"

        ''' <summary>
        ''' Fired when the conditional compilations constants textbox has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values
        ''' </summary>
        Private Sub DefineConstants_TextChanged(sender As Object, e As EventArgs) Handles txtConditionalCompilationSymbols.TextChanged
            If Not InsideInternalUpdate Then
                Debug.Assert(CondCompSymbols IsNot Nothing)
                For i As Integer = 0 To CondCompSymbols.Length - 1
                    'Parse the original compilation constants value for this configuration (we need to do this
                    '  to get the original DEBUG/TRACE values for these configurations - we can't rely on the
                    '  current control values for these two because they might be indeterminate)
                    Dim OriginalOtherConstants As String = ""
                    Dim DebugDefined, TraceDefined As Boolean
                    ParseConditionalCompilationConstants(CondCompSymbols(i), DebugDefined, TraceDefined, OriginalOtherConstants)

                    'Now build the new string based off of the old DEBUG/TRACE values and the new string the user entered for any
                    '  other constants
                    Dim NewOtherConstants As String = txtConditionalCompilationSymbols.Text
                    Dim NewCondCompSymbols As String = NewOtherConstants
                    If DebugDefined Then
                        NewCondCompSymbols = AddSymbol(NewCondCompSymbols, Const_CondConstantDEBUG)
                    End If
                    If TraceDefined Then
                        NewCondCompSymbols = AddSymbol(NewCondCompSymbols, Const_CondConstantTRACE)
                    End If

                    '... and store it
                    CondCompSymbols(i) = NewCondCompSymbols
                Next

                'No need to mark the property dirty - the property page architecture hooks up the FormControl automatically
                '  to TextChanged and will mark it dirty, and will make sure it's persisted on LostFocus.
            End If
        End Sub

        ''' <summary>
        ''' Fired when the "Define DEBUG Constant" check has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values.
        ''' </summary>
        Private Sub chkDefineDebug_CheckedChanged(sender As Object, e As EventArgs) Handles chkDefineDebug.CheckedChanged
            If Not InsideInternalUpdate Then
                Dim DebugIndexDoNotChange As Integer 'Index to avoid changing, if in simplified configs mode
                If IsSimplifiedConfigs() Then
                    'In simplified configs mode, we do not want to change the value of the DEBUG constant
                    '  in the Debug configuration, but rather only in the Release configuration
                    Debug.Assert(CondCompSymbols.Length = 2, "In simplified configs, we should only have two configurations")
                    DebugIndexDoNotChange = GetIndexOfConfiguration(Const_DebugConfiguration)
                Else
                    DebugIndexDoNotChange = -1 'Go ahead and make changes in all selected configurations
                End If

                For i As Integer = 0 To CondCompSymbols.Length - 1
                    If i <> DebugIndexDoNotChange Then
                        Select Case chkDefineDebug.CheckState
                            Case CheckState.Checked
                                'Make sure DEBUG is present in the configuration
                                CondCompSymbols(i) = AddSymbol(CondCompSymbols(i), Const_CondConstantDEBUG)
                            Case CheckState.Unchecked
                                'Remove DEBUG from the configuration
                                CondCompSymbols(i) = RemoveSymbol(CondCompSymbols(i), Const_CondConstantDEBUG)
                            Case Else
                                Debug.Fail("If the user changed the checked state, it should be checked or unchecked")
                        End Select
                    End If
                Next

                SetDirty(VsProjPropId.VBPROJPROPID_DefineConstants, True)
            End If
        End Sub

        ''' <summary>
        ''' Fired when the "Define DEBUG Constant" check has changed.  We are manually handling
        '''   events associated with this control, so we need to recalculate related values.
        ''' </summary>
        Private Sub chkDefineTrace_CheckedChanged(sender As Object, e As EventArgs) Handles chkDefineTrace.CheckedChanged
            If Not InsideInternalUpdate Then
                For i As Integer = 0 To CondCompSymbols.Length - 1
                    Select Case chkDefineTrace.CheckState
                        Case CheckState.Checked
                            'Make sure TRACE is present in the configuration
                            CondCompSymbols(i) = AddSymbol(CondCompSymbols(i), Const_CondConstantTRACE)
                        Case CheckState.Unchecked
                            'Remove TRACE from the configuration
                            CondCompSymbols(i) = RemoveSymbol(CondCompSymbols(i), Const_CondConstantTRACE)
                        Case Else
                            Debug.Fail("If the user changed the checked state, it should be checked or unchecked")
                    End Select
                Next

                SetDirty(VsProjPropId.VBPROJPROPID_DefineConstants, True)
            End If
        End Sub

        ''' <summary>
        ''' Given DefineConstants string, parse it into a DEBUG value, a TRACE value, and everything else
        ''' </summary>
        Private Shared Sub ParseConditionalCompilationConstants(DefineConstantsFullValue As String, ByRef DebugDefined As Boolean, ByRef TraceDefined As Boolean, ByRef OtherConstants As String)
            'Start out with the full set of defined constants
            OtherConstants = DefineConstantsFullValue

            'Check for DEBUG
            If FindSymbol(OtherConstants, Const_CondConstantDEBUG) Then
                DebugDefined = True

                'Strip it out
                OtherConstants = RemoveSymbol(OtherConstants, Const_CondConstantDEBUG)
            Else
                DebugDefined = False
            End If

            'Check for TRACE
            If FindSymbol(OtherConstants, Const_CondConstantTRACE) Then
                TraceDefined = True

                'Strip it out
                OtherConstants = RemoveSymbol(OtherConstants, Const_CondConstantTRACE)
            Else
                TraceDefined = False
            End If
        End Sub

        ''' <summary>
        ''' Multi-value setter for the conditional compilation constants value.  We parse the values and determine
        '''   what to display in the textbox and checkboxes.
        ''' </summary>
        Private Function ConditionalCompilationSet(control As Control, prop As PropertyDescriptor, values() As Object) As Boolean
            Debug.Assert(values IsNot Nothing)
#If DEBUG Then
            For i As Integer = 0 To values.Length - 1
                Debug.Assert(values(i) IsNot Nothing)
                Debug.Assert(Not PropertyControlData.IsSpecialValue(values(i)))
            Next

            If Switches.PDProperties.TraceInfo Then
                Switches.TracePDProperties(TraceLevel.Info, "ConditionalCompilationSet: Initial Values:")
                For i As Integer = 0 To values.Length - 1
                    Switches.TracePDProperties(TraceLevel.Info, "  Value #" & i & ": " & DebugToString(values(i)))
                Next
            End If
#End If

            'Store off the conditional full (unparsed) compilation strings, we'll need this in the getter (because the
            '  values displayed in the controls are lossy when there are indeterminate values).
            ReDim CondCompSymbols(values.Length - 1)
            values.CopyTo(CondCompSymbols, 0)

            InsideInternalUpdate = True
            Try
                Dim DebugDefinedValues(values.Length - 1) As Object 'Defined as object so we can use GetValueOrIndeterminateFromArray
                Dim TraceDefinedValues(values.Length - 1) As Object
                Dim OtherConstantsValues(values.Length - 1) As String

                'Parse out each individual set of DefineConstants values from the project
                For i As Integer = 0 To values.Length - 1
                    Dim FullDefineConstantsValue As String = DirectCast(values(i), String)
                    Dim DebugDefinedValue, TraceDefinedValue As Boolean
                    Dim OtherConstantsValue As String = ""

                    ParseConditionalCompilationConstants(FullDefineConstantsValue, DebugDefinedValue, TraceDefinedValue, OtherConstantsValue)
                    DebugDefinedValues(i) = DebugDefinedValue
                    TraceDefinedValues(i) = TraceDefinedValue
                    OtherConstantsValues(i) = OtherConstantsValue
                Next

                'Figure out whether the values each configuration are the same or different.  For each
                '  of these properties, get either the value which is the same across all of the values,
                '  or get a value of Indeterminate.
                Dim DebugDefined As Object = PropertyControlData.GetValueOrIndeterminateFromArray(DebugDefinedValues)
                Dim TraceDefined As Object = PropertyControlData.GetValueOrIndeterminateFromArray(TraceDefinedValues)
                Dim OtherConstants As Object = PropertyControlData.GetValueOrIndeterminateFromArray(OtherConstantsValues)

                If IsSimplifiedConfigs() Then
                    'Special behavior for simplified configurations - we want to only display the
                    '  release value of the DEBUG checkbox.
                    Dim ReleaseIndex As Integer = GetIndexOfConfiguration(Const_ReleaseConfiguration)
                    If ReleaseIndex >= 0 Then
                        DebugDefined = DebugDefinedValues(ReleaseIndex) 'Get the release-config value for DEBUG constant
                    End If
                End If

                'Finally, set the control values to their calculated state
                If PropertyControlData.IsSpecialValue(DebugDefined) Then
                    chkDefineDebug.CheckState = CheckState.Indeterminate
                Else
                    SetCheckboxDeterminateState(chkDefineDebug, CBool(DebugDefined))
                End If
                If PropertyControlData.IsSpecialValue(TraceDefined) Then
                    chkDefineTrace.CheckState = CheckState.Indeterminate
                Else
                    SetCheckboxDeterminateState(chkDefineTrace, CBool(TraceDefined))
                End If
                If PropertyControlData.IsSpecialValue(OtherConstants) Then
                    txtConditionalCompilationSymbols.Text = ""
                Else
                    txtConditionalCompilationSymbols.Text = DirectCast(OtherConstants, String)
                End If

            Finally
                InsideInternalUpdate = False
            End Try

            Return True
        End Function

        ''' <summary>
        ''' Multi-value getter for the conditional compilation constants values.
        ''' </summary>
        Private Function ConditionalCompilationGet(control As Control, prop As PropertyDescriptor, ByRef values() As Object) As Boolean
            'Fetch the original values we stored in the setter (the values stored in the controls are lossy when there are indeterminate values)
            Debug.Assert(CondCompSymbols IsNot Nothing)
            ReDim values(CondCompSymbols.Length - 1)
            CondCompSymbols.CopyTo(values, 0)
            Return True
        End Function

        ''' <summary>
        ''' Searches in the RawPropertiesObjects for a configuration object whose name matches the name passed in,
        '''   and returns the index to it.
        ''' </summary>
        ''' <param name="ConfigurationName"></param>
        ''' <returns>The index of the found configuration, or -1 if it was not found.</returns>
        ''' <remarks>
        ''' We're only guaranteed to find the "Debug" or "Release" configurations when in
        '''   simplified configuration mode.
        ''' </remarks>
        Private Function GetIndexOfConfiguration(ConfigurationName As String) As Integer
            Debug.Assert(IsSimplifiedConfigs, "Shouldn't be calling this in advanced configs mode - not guaranteed to have Debug/Release configurations")

            Dim DefineConstantsData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants)
            Debug.Assert(DefineConstantsData IsNot Nothing)

            Dim Objects() As Object = RawPropertiesObjects(DefineConstantsData)
            Dim Index As Integer = 0
            For Each Obj As Object In Objects
                Debug.Assert(Obj IsNot Nothing, "Why was Nothing passed in as a config object?")
                Dim Config As IVsCfg = TryCast(Obj, IVsCfg)
                Debug.Assert(Config IsNot Nothing, "Object was not IVsCfg")
                If Config IsNot Nothing Then
                    Dim ConfigName As String = Nothing
                    Dim PlatformName As String = Nothing
                    ShellUtil.GetConfigAndPlatformFromIVsCfg(Config, ConfigName, PlatformName)
                    If ConfigurationName.Equals(ConfigName, StringComparison.CurrentCultureIgnoreCase) Then
                        'Found it - return the index to it
                        Return Index
                    End If
                End If
                Index += 1
            Next

            Debug.Fail("Unable to find the configuration '" & ConfigurationName & "'")
            Return -1
        End Function

        ''' <summary>
        ''' Returns whether or not we're in simplified config mode for this project, which means that
        '''   we hide the configuration/platform comboboxes.
        ''' </summary>
        Public Function IsSimplifiedConfigs() As Boolean
            Return ShellUtil.GetIsSimplifiedConfigMode(ProjectHierarchy)
        End Function

        ''' <summary>
        ''' Given a string containing conditional compilation constants, adds the given constant to it, if it
        '''   doesn't already exist.
        ''' </summary>
        Public Shared Function AddSymbol(stOldCondCompConstants As String, stSymbol As String) As String
            ' See if we find it
            Dim rgConstants() As String
            Dim bFound As Boolean = False

            If Not IsNothing(stOldCondCompConstants) Then
                rgConstants = stOldCondCompConstants.Split(New Char() {";"c})

                Dim stTemp As String

                If Not IsNothing(rgConstants) Then
                    For Each stTemp In rgConstants
                        If String.Equals(Trim(stTemp), stSymbol, StringComparison.Ordinal) Then
                            bFound = True
                            Exit For
                        End If
                    Next
                End If
            End If

            If Not bFound Then
                ' Add it to the beginning
                Dim stNewConstants As String = stSymbol

                If stOldCondCompConstants <> "" Then
                    stNewConstants += ";"
                End If
                stNewConstants += stOldCondCompConstants

                Return stNewConstants
            Else
                Return stOldCondCompConstants
            End If
        End Function

        ''' <summary>
        ''' Given a string containing conditional compilation constants, determines if the given constant is defined in it
        ''' </summary>
        Public Shared Function FindSymbol(stOldCondCompConstants As String, stSymbol As String) As Boolean
            ' See if we find it
            Dim rgConstants() As String

            If Not IsNothing(stOldCondCompConstants) Then
                rgConstants = stOldCondCompConstants.Split(New Char() {";"c})

                Dim stTemp As String

                If Not IsNothing(rgConstants) Then
                    For Each stTemp In rgConstants
                        If String.Equals(Trim(stTemp), stSymbol, StringComparison.Ordinal) Then
                            Return True
                        End If
                    Next
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Given a string containing conditional compilation constants, removes the given constant from it, if it
        '''   is in the list.
        ''' </summary>
        Public Shared Function RemoveSymbol(stOldCondCompConstants As String, stSymbol As String) As String
            ' Look for the DEBUG constant
            Dim rgConstants() As String
            Dim stNewConstants As String = ""

            If Not IsNothing(stOldCondCompConstants) Then
                rgConstants = stOldCondCompConstants.Split(New Char() {";"c})

                Dim stTemp As String

                If Not IsNothing(rgConstants) Then
                    For Each stTemp In rgConstants
                        If Not String.Equals(Trim(stTemp), stSymbol, StringComparison.Ordinal) Then
                            If stNewConstants <> "" Then
                                stNewConstants += ";"
                            End If

                            stNewConstants += stTemp
                        End If
                    Next
                End If
            Else
                stNewConstants = ""
            End If

            Return stNewConstants
        End Function

#End Region

        Private Class ComboItem

            ''' <summary>
            ''' Stores the property value
            ''' </summary>
            Private ReadOnly _value As String

            ''' <summary>
            ''' Stores the display name
            ''' </summary>
            Private ReadOnly _displayName As String

            ''' <summary>
            ''' Constructor that uses the provided value and display name
            ''' </summary>
            Friend Sub New(value As String, displayName As String)

                _value = value
                _displayName = displayName

            End Sub

            ''' <summary>
            ''' Gets the value
            ''' </summary>
            Public ReadOnly Property Value As String
                Get
                    Return _value
                End Get
            End Property

            ''' <summary>
            ''' Use the display name for the string display
            ''' </summary>
            Public Overrides Function ToString() As String
                Return _displayName
            End Function

        End Class
    End Class

End Namespace
