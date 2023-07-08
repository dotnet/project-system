' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Configuration
Imports System.Globalization
Imports System.Web.ClientServices.Providers
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.Xml
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Editors.PropertyPages
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VSDesigner
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The user control that allows the user to interact with the designed component
    ''' For each row in the grid, the Tag property should be set to a corresponding 
    ''' DesignTimeSettingInstance
    ''' </summary>
    Friend NotInheritable Class SettingsDesignerView
        Inherits BaseDesignerView
        Implements IVsWindowPaneCommit
        Implements IVsBroadcastMessageEvents

        Private Const NameColumnNo As Integer = 0
        Private Const TypeColumnNo As Integer = 1
        Private Const ScopeColumnNo As Integer = 2
        Private Const ValueColumnNo As Integer = 3
        Private _menuCommands As ArrayList
        Private _accessModifierCombobox As SettingsDesignerAccessModifierCombobox

#Region "Nested Class for the 'Access modifier' dropdown'"

        Friend Class SettingsDesignerAccessModifierCombobox
            Inherits AccessModifierCombobox

            Public Sub New(rootDesigner As BaseRootDesigner, serviceProvider As IServiceProvider, projectItem As EnvDTE.ProjectItem, namespaceToOverrideIfCustomToolIsEmpty As String)
                MyBase.New(rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)

                AddCodeGeneratorEntry(AccessModifierType.Internal, SettingsSingleFileGenerator.SingleFileGeneratorName)
                AddCodeGeneratorEntry(AccessModifierType.Public, PublicSettingsSingleFileGenerator.SingleFileGeneratorName)

                'Make sure both the internal and public custom tool values are "recognized"
                AddRecognizedCustomToolValue(SettingsSingleFileGenerator.SingleFileGeneratorName)
                AddRecognizedCustomToolValue(PublicSettingsSingleFileGenerator.SingleFileGeneratorName)
            End Sub

            Public Shadows Function GetMenuCommandsToRegister() As ICollection
                Return MyBase.GetMenuCommandsToRegister(
                    Constants.MenuConstants.CommandIDSettingsDesignerAccessModifierCombobox,
                    Constants.MenuConstants.CommandIDSettingsDesignerGetAccessModifierOptions)
            End Function

            Protected Overrides Function IsDesignerEditable() As Boolean
                'UNDONE: test SCC checkout
                Dim designerLoader As SettingsDesignerLoader = TryCast(RootDesigner.GetService(GetType(IDesignerLoaderService)), SettingsDesignerLoader)
                If designerLoader Is Nothing Then
                    Debug.Fail("Failed to get the designer loader")
                    Return False
                End If

                Return designerLoader.IsDesignerEditable()
            End Function
        End Class

        ''' <summary>
        ''' Wrapper class that has the ability to indicate to the settings designer view
        ''' that it is a really bad time to change the current cell while already committing
        ''' changes...
        ''' </summary>
        Friend Class SettingsGridView
            Inherits DesignerDataGridView

            Private _committingChanges As Boolean

            Friend Property CommittingChanges As Boolean
                Get
                    Return _committingChanges
                End Get
                Set
                    _committingChanges = Value
                End Set
            End Property
        End Class

#End Region

        ' The "actual" grid containing all settings
        Private WithEvents _settingsGridView As SettingsGridView

        ' Padding used to calculate width of comboboxes to avoid getting the text
        ' truncated...
        Private Const InternalComboBoxPadding As Integer = 10

        Private WithEvents _dataGridViewTextBoxColumn1 As DataGridViewTextBoxColumn
        Private WithEvents _dataGridViewComboBoxColumn1 As DataGridViewComboBoxColumn
        Private WithEvents _dataGridViewComboBoxColumn2 As DataGridViewComboBoxColumn
        Private WithEvents _descriptionLinkLabel As VSThemedLinkLabel

        Private _suppressValidationUI As Boolean
        Private WithEvents _settingsTableLayoutPanel As TableLayoutPanel
        Private _isReportingError As Boolean
        Private _isShowingTypePicker As Boolean
        Private ReadOnly _toolbarPanel As DesignerToolbarPanel

        ' Does the current language support partial classes? If yes, enable the ViewCode button...
        Private _viewCodeEnabled As Boolean
        Private _cachedCodeProvider As CodeDom.Compiler.CodeDomProvider

        ' Does the project system support user scoped settings?
        Private _projectSystemSupportsUserScope As Boolean

        'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
        Private _cookieBroadcastMessages As UInteger

        ' Prevent recursive validation (sometimes we do things in cell validated that causes the
        ' focus to move, which causes additional cellvalidated events)
        Private _inCellValidated As Boolean

        ''' <summary>
        ''' Cached instance of the type name resolution service
        ''' </summary>
        Private _typeNameResolver As SettingTypeNameResolutionService

        ''' <summary>
        ''' Cached instance of the setting type cache service
        ''' </summary>
        Private _settingTypeCache As SettingsTypeCache

        ''' <summary>
        ''' Cached instance of the setting value cache
        ''' </summary>
        Private _valueCache As SettingsValueCache

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            SuspendLayout()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            _settingsTableLayoutPanel.SuspendLayout()

            _settingsGridView.Columns(NameColumnNo).HeaderText = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_GridViewNameColumnHeaderText
            _settingsGridView.Columns(NameColumnNo).CellTemplate = New DesignerDataGridView.EditOnClickDataGridViewTextBoxCell()
            _settingsGridView.Columns(TypeColumnNo).HeaderText = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_GridViewTypeColumnHeaderText
            _settingsGridView.Columns(TypeColumnNo).CellTemplate = New DesignerDataGridView.EditOnClickDataGridViewComboBoxCell()
            _settingsGridView.Columns(ScopeColumnNo).HeaderText = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_GridViewScopeColumnHeaderText
            _settingsGridView.Columns(ScopeColumnNo).CellTemplate = New DesignerDataGridView.EditOnClickDataGridViewComboBoxCell()

            Dim TypeEditorCol As New DataGridViewUITypeEditorColumn With {
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                .FillWeight = 100.0!,
                .HeaderText = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_GridViewValueColumnHeaderText,
                .MinimumWidth = DpiAwareness.LogicalToDeviceUnits(Handle, SystemInformation.VerticalScrollBarWidth + 2), ' Add 2 for left/right borders...
                .Resizable = DataGridViewTriState.True,
                .SortMode = DataGridViewColumnSortMode.Automatic,
                .Width = DpiAwareness.LogicalToDeviceUnits(Handle, 200)
            }
            _settingsGridView.Columns.Add(TypeEditorCol)

            _settingsGridView.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            _settingsGridView.Text = "m_SettingsGridView"
            _settingsGridView.DefaultCellStyle.NullValue = ""

            ScopeColumn.Items.Add(DesignTimeSettingInstance.SettingScope.Application)
            ScopeColumn.Items.Add(DesignTimeSettingInstance.SettingScope.User)

            SetLinkLabelText()

            _settingsGridView.ColumnHeadersHeight = _settingsGridView.Rows(0).GetPreferredHeight(0, DataGridViewAutoSizeRowMode.AllCells, False)
            AddHandler _settingsGridView.KeyDown, AddressOf OnGridKeyDown

            _toolbarPanel = New DesignerToolbarPanel With {
                .Name = "ToolbarPanel",
                .Text = "ToolbarPanel"
            }
            _settingsTableLayoutPanel.Controls.Add(_toolbarPanel, 0, 0)
            _settingsTableLayoutPanel.ResumeLayout()
            ResumeLayout()
        End Sub

        Private Sub OnGridKeyDown(s As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Tab Then
                ' Tab key shouldn't be used to move us to the next cell. Otherwise we can't leave the grid view without traversing
                ' the whole table with Tab key (even then, we get stuck at the last cell).
                ' Tab key should instead get us to the next tab stop, making all the controls accessible by keyboard.
                ' Moving between cells can be done using arrow keys.
                _descriptionLinkLabel.Focus()
                e.Handled = True
            End If
        End Sub

        ''' <summary>
        ''' Dispose my resources
        ''' </summary>
        ''' <param name="disposing"></param>
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _accessModifierCombobox IsNot Nothing Then
                    _accessModifierCombobox.Dispose()
                End If

                If _cookieBroadcastMessages <> 0 Then
                    Dim VsShell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                    If VsShell IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(VsShell.UnadviseBroadcastMessages(_cookieBroadcastMessages))
                        _cookieBroadcastMessages = 0
                    End If
                End If

                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
                ' Forget about any component change service
                ChangeService = Nothing

                ' Remove any dependencies on the current settings instance...
                Settings = Nothing

            End If
            ' Don't forget to let my base dispose itself
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private ReadOnly _components As IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Private Sub InitializeComponent()
            Dim resources As ComponentResourceManager = New ComponentResourceManager(GetType(SettingsDesignerView))
            _settingsGridView = New SettingsGridView
            BackColor = ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, Drawing.SystemColors.ButtonFace, UseVSTheme:=False)
            _descriptionLinkLabel = New VSThemedLinkLabel
            _dataGridViewTextBoxColumn1 = New DataGridViewTextBoxColumn
            _dataGridViewComboBoxColumn1 = New DataGridViewComboBoxColumn
            _dataGridViewComboBoxColumn2 = New DataGridViewComboBoxColumn
            _settingsTableLayoutPanel = New TableLayoutPanel
            CType(_settingsGridView, ISupportInitialize).BeginInit()
            _settingsTableLayoutPanel.SuspendLayout()
            SuspendLayout()
            '
            'm_SettingsGridView
            '
            resources.ApplyResources(_settingsGridView, "m_SettingsGridView")
            _settingsGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            _settingsGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            _settingsGridView.BackgroundColor = ShellUtil.GetVSColor(__VSSYSCOLOREX3.VSCOLOR_THREEDFACE, Drawing.SystemColors.ButtonFace, UseVSTheme:=False)
            _settingsGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
            _settingsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            _settingsGridView.Columns.Add(_dataGridViewTextBoxColumn1)
            _settingsGridView.Columns.Add(_dataGridViewComboBoxColumn1)
            _settingsGridView.Columns.Add(_dataGridViewComboBoxColumn2)
            resources.ApplyResources(_settingsGridView, "m_SettingsGridView")
            _settingsGridView.Margin = New Padding(14)
            _settingsGridView.Name = "m_SettingsGridView"
            _settingsGridView.TabStop = True
            '
            'DataGridViewTextBoxColumn1
            '
            resources.ApplyResources(_dataGridViewTextBoxColumn1, "DataGridViewTextBoxColumn1")
            _dataGridViewTextBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            _dataGridViewTextBoxColumn1.MinimumWidth = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            _dataGridViewTextBoxColumn1.Name = "GridViewNameTextBoxColumn"
            _dataGridViewComboBoxColumn1.Width = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            '
            'DataGridViewComboBoxColumn1
            '
            resources.ApplyResources(_dataGridViewComboBoxColumn1, "DataGridViewComboBoxColumn1")
            _dataGridViewComboBoxColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            _dataGridViewComboBoxColumn1.MinimumWidth = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            _dataGridViewComboBoxColumn1.Name = "GridViewTypeComboBoxColumn"
            _dataGridViewComboBoxColumn1.SortMode = DataGridViewColumnSortMode.Automatic
            _dataGridViewComboBoxColumn1.Width = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            '
            'DataGridViewComboBoxColumn2
            '
            _dataGridViewComboBoxColumn2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            resources.ApplyResources(_dataGridViewComboBoxColumn2, "DataGridViewComboBoxColumn2")
            _dataGridViewComboBoxColumn2.MaxDropDownItems = 2
            _dataGridViewComboBoxColumn2.MinimumWidth = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            _dataGridViewComboBoxColumn2.Name = "GridViewScopeComboBoxColumn"
            _dataGridViewComboBoxColumn2.SortMode = DataGridViewColumnSortMode.Automatic
            _dataGridViewComboBoxColumn2.ValueType = GetType(DesignTimeSettingInstance.SettingScope)
            _dataGridViewComboBoxColumn2.Width = DpiAwareness.LogicalToDeviceUnits(Handle, 100)
            '
            'DescriptionLinkLabel
            '
            resources.ApplyResources(_descriptionLinkLabel, "DescriptionLinkLabel")
            _descriptionLinkLabel.Margin = New Padding(14, 23, 14, 9)
            _descriptionLinkLabel.Name = "DescriptionLinkLabel"
            _descriptionLinkLabel.DisplayFocusCues = True
            _descriptionLinkLabel.TabStop = True
            '
            'SettingsTableLayoutPanel
            '
            _settingsTableLayoutPanel.ColumnCount = 1
            _settingsTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            _settingsTableLayoutPanel.Controls.Add(_settingsGridView, 0, 2)
            _settingsTableLayoutPanel.Controls.Add(_descriptionLinkLabel, 0, 1)
            resources.ApplyResources(_settingsTableLayoutPanel, "SettingsTableLayoutPanel")
            _settingsTableLayoutPanel.Margin = New Padding(0)
            _settingsTableLayoutPanel.Name = "SettingsTableLayoutPanel"
            _settingsTableLayoutPanel.RowCount = 3
            _settingsTableLayoutPanel.RowStyles.Add(New RowStyle)
            _settingsTableLayoutPanel.RowStyles.Add(New RowStyle)
            _settingsTableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))
            _settingsTableLayoutPanel.TabStop = True
            '
            'SettingsDesignerView
            '
            Controls.Add(_settingsTableLayoutPanel)
            AutoScaleMode = AutoScaleMode.Font
            Margin = New Padding(0)
            Name = "SettingsDesignerView"
            Padding = New Padding(0)
            resources.ApplyResources(Me, "$this")
            CType(_settingsGridView, ISupportInitialize).EndInit()
            _settingsTableLayoutPanel.ResumeLayout(False)
            ResumeLayout(False)

        End Sub

#End Region

#Region "Private fields"

        ''' <summary>
        ''' Reference to "our" root designer
        ''' </summary>
        Private _rootDesigner As SettingsDesigner

        ''' <summary>
        ''' The settings we show in the grid
        ''' </summary>
        Private _settingsProperty As DesignTimeSettings

        ' Private cached service
        Private _designerLoader As SettingsDesignerLoader

        ' Cached IVsHierarchy
        Private _hierarchy As IVsHierarchy

#End Region

        ''' <summary>
        ''' Set the designer associated with this view
        ''' </summary>
        ''' <param name="Designer"></param>
        ''' <remarks>
        ''' When setting the designer, a complete refresh of the view is performed.
        ''' </remarks>
        Public Sub SetDesigner(Designer As SettingsDesigner)
            Dim types As IEnumerable(Of Type)
            If _rootDesigner IsNot Nothing Then
                UnregisterMenuCommands(_rootDesigner)
            End If
            _rootDesigner = Designer

            Debug.Assert(Designer IsNot Nothing)
            Debug.Assert(DesignerLoader IsNot Nothing)
            Debug.Assert(DesignerLoader.ProjectItem IsNot Nothing)
            Debug.Assert(DesignerLoader.VsHierarchy IsNot Nothing)
            _accessModifierCombobox = New SettingsDesignerAccessModifierCombobox(
                Designer,
                Designer,
                DesignerLoader.ProjectItem,
                IIf(IsVbProject(DesignerLoader.VsHierarchy), SettingsSingleFileGeneratorBase.MyNamespaceName, Nothing))

            _valueCache = DirectCast(GetService(GetType(SettingsValueCache)), SettingsValueCache)
            _settingTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)

            _typeNameResolver = DirectCast(GetService(GetType(SettingTypeNameResolutionService)), SettingTypeNameResolutionService)
            Debug.Assert(_typeNameResolver IsNot Nothing, "The settings designer loader should have added a typenameresolver component!")

            ' Add all the (currently) known types 
            TypeColumn.Items.Clear()
            types = _settingTypeCache.GetWellKnownTypes()
            For Each t As Type In types
                TypeColumn.Items.Add(_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(t.FullName))
            Next

            ' Make sure the "normal" types are sorted...
            TypeColumn.Sorted = True
            TypeColumn.Sorted = False

            ' Add the "connection string" pseudo type
            TypeColumn.Items.Add(_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(SettingsSerializer.CultureInvariantVirtualTypeNameWebReference))
            TypeColumn.Items.Add(_typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString))

            TypeColumn.Width = DpiAwareness.LogicalToDeviceUnits(Handle, TypeColumn.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, False) + SystemInformation.VerticalScrollBarWidth + InternalComboBoxPadding)

            ScopeColumn.Width = DpiAwareness.LogicalToDeviceUnits(Handle, ScopeColumn.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, False) + SystemInformation.VerticalScrollBarWidth + InternalComboBoxPadding)

            'Hook up for broadcast messages
            If _cookieBroadcastMessages = 0 Then
                Dim VSShell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                If VSShell IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VSShell.AdviseBroadcastMessages(Me, _cookieBroadcastMessages))
                Else
                    Debug.Fail("Unable to get IVsShell for broadcast messages")
                End If
            End If
            SetFonts()

            If Designer.Settings IsNot Nothing AndAlso Designer.Settings.Site IsNot Nothing Then
                _hierarchy = DirectCast(Designer.Settings.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Else
                _hierarchy = Nothing
            End If

            If _hierarchy IsNot Nothing Then
                _projectSystemSupportsUserScope = Not ShellUtil.IsWebProject(_hierarchy)
            Else
                _projectSystemSupportsUserScope = True
            End If

            ' Do not allow browsing or serializing arbitrary types for .NET Core scenarios.
            ' We don't currently have a general mechanism to identify types known to both the
            ' designer (running on .NET Framework) and the application (running on .NET Core).
            Dim multiTargetService = New MultiTargetService(_hierarchy, VSConstants.VSITEMID_ROOT, False)
            If (multiTargetService.TargetFrameworkName.Identifier <> ".NETCoreApp") Then
                TypeColumn.Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_BrowseType)
            End If

            Settings = Designer.Settings

            ' ...get new changes service...
            ChangeService = DirectCast(Designer.GetService(GetType(IComponentChangeService)), IComponentChangeService)

            Dim VsUIShell As IVsUIShell = DirectCast(Designer.GetService(GetType(IVsUIShell)), IVsUIShell)

            ' Register menu commands...
            RegisterMenuCommands(Designer)
            _toolbarPanel.SetToolbar(VsUIShell, Constants.MenuConstants.GUID_SETTINGSDESIGNER_MenuGroup, Constants.MenuConstants.IDM_VS_TOOLBAR_Settings)
            _toolbarPanel.BringToFront()

            _descriptionLinkLabel.SetThemedColor(TryCast(VsUIShell, IVsUIShell5), supportsTheming:=False)

        End Sub

        ''' <summary>
        ''' Gets the environment font for the shell.
        ''' </summary>
        Private Function GetEnvironmentFont() As Drawing.Font
            If UIService IsNot Nothing Then
                Dim Font As Drawing.Font = DirectCast(UIService.Styles("DialogFont"), Drawing.Font)
                Debug.Assert(Font IsNot Nothing, "Unable to get dialog font from IUIService")
                Return Font
            Else
                Debug.Fail("Unable to get IUIService for dialog font")
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Set the localized text and link part of the link label
        ''' </summary>
        Private Sub SetLinkLabelText()
            Dim fullText As String = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_FullDescriptionText
            Dim linkText As String = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_LinkPartOfDescriptionText

            ' Adding two spaces and including the first space in the link due to VsWhidbey 482875
            _descriptionLinkLabel.Text = fullText & "  " & linkText

            _descriptionLinkLabel.Links.Clear()

            ' Adding one to the length of the linkText 'cause we have included one of the two leading spaces
            ' in the link (see above)
            _descriptionLinkLabel.Links.Add(fullText.Length() + 1, linkText.Length + 1)
        End Sub

        ''' <summary>
        ''' Pop up the appropriate help context when the user clicks on the description link
        ''' </summary>
        Private Sub DescriptionLinkLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles _descriptionLinkLabel.LinkClicked
            DesignUtil.DisplayTopicFromF1Keyword(_rootDesigner, HelpIDs.SettingsDesignerDescription)
        End Sub

        ''' <summary>
        ''' Initialize the fonts in the resource editor from the environment (or from the resx file,
        '''   if hard-coded there).
        ''' </summary>
        Private Sub SetFonts()
            Dim DialogFont As Drawing.Font = GetEnvironmentFont()
            If DialogFont IsNot Nothing Then
                Font = DialogFont
            End If

            SetComboBoxColumnDropdownWidth(TypeColumn)
            SetComboBoxColumnDropdownWidth(ScopeColumn)
        End Sub

        ''' <summary>
        ''' The settings we are currently displaying in the grid...
        ''' </summary>
        Private Property Settings As DesignTimeSettings
            Get
                Return _settingsProperty
            End Get
            Set
                ' Setting the settings to the same instance is a NOOP
                If Value IsNot Settings Then
                    ' Store this value for later use!
                    _settingsProperty = Value

                    If Settings IsNot Nothing Then
                        RefreshGrid()
                        ' We want to give the name column a reasonable start width. If we did this by using auto fill/fill weight, 
                        ' changing the value column would change the size of the name column, which looks weird. We'll just default the
                        ' size to 1/3 of the value column's width and leave it at that (the user can resize if they want to)
                        _settingsGridView.Columns(NameColumnNo).Width = CInt(TypeColumn.Width / 2)
                    End If
                End If
            End Set
        End Property

        Private _changeService As IComponentChangeService
        ''' <summary>
        ''' Our cached component change service
        ''' </summary>
        ''' <remarks>
        ''' Will unhook event handlers from old component changed service and hook up handlers
        ''' to the new service
        '''</remarks>
        Friend Property ChangeService As IComponentChangeService
            Get
                Return _changeService
            End Get
            Set
                If Value IsNot _changeService Then
                    UnSubscribeChangeServiceNotifications()
                    _changeService = Value
                    SubscribeChangeServiceNotifications()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Hook up component changed/added/removed event handlers
        ''' </summary>
        Private Sub SubscribeChangeServiceNotifications()
            If ChangeService IsNot Nothing Then
                AddHandler ChangeService.ComponentChanged, AddressOf ComponentChangedHandler
                AddHandler ChangeService.ComponentRemoved, AddressOf ComponentRemovedHandler
                AddHandler ChangeService.ComponentAdded, AddressOf ComponentAddedHandler
            End If
        End Sub

        ''' <summary>
        ''' Unhook component changed/added/removed event handlers
        ''' </summary>
        Private Sub UnSubscribeChangeServiceNotifications()
            If ChangeService IsNot Nothing Then
                RemoveHandler ChangeService.ComponentChanged, AddressOf ComponentChangedHandler
                RemoveHandler ChangeService.ComponentRemoved, AddressOf ComponentRemovedHandler
                RemoveHandler ChangeService.ComponentAdded, AddressOf ComponentAddedHandler
            End If
        End Sub

        ''' <summary>
        ''' A component in our hosts container has changed
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentChangedHandler(Sender As Object, e As ComponentChangedEventArgs)
            ' There is a slight possibility that we'll be called after the designer has been disposed (a web reference
            ' rename can cause a project file checkout, which may cause a project reload, which will dispose us and
            ' once it is our turn to get the component change notification, we are already disposed)
            ' 
            ' Fortunately, the fix is easy - we only need to bail if we have already been disposed....
            If IsDisposed Then
                Return
            End If

            Dim designTimeSettingInstance = TryCast(e.Component, DesignTimeSettingInstance)
            If (designTimeSettingInstance IsNot Nothing) Then
                Dim Row As DataGridViewRow = RowFromComponent(designTimeSettingInstance)
                If Row Is Nothing Then
                    Debug.Fail("ComponentChanged: Failed to find row...")
                Else
                    SetUIRowValues(Row, designTimeSettingInstance)
                End If
            End If
        End Sub

        ''' <summary>
        ''' A component was removed from our hosts container
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentRemovedHandler(Sender As Object, e As ComponentEventArgs)
            If TypeOf e.Component Is DesignTimeSettingInstance Then
                ' This was a setting instance - let's find the corresponding row and
                ' remove it from the grid
                Dim Row As DataGridViewRow = RowFromComponent(DirectCast(e.Component, DesignTimeSettingInstance))
                If Row IsNot Nothing Then
                    If Row.Index = _settingsGridView.RowCount - 1 Then
                        'This is the "new row" - it can't be removed 
                        Row.Tag = Nothing
                    Else
                        ' If we are currently editing something while the row is removed,
                        ' then we should cancel the edit. 
                        ' If not, we may run into issues like described in DevDiv 85344
                        If _settingsGridView.IsCurrentCellInEditMode Then
                            _settingsGridView.CancelEdit()
                        End If
                        _settingsGridView.Rows.Remove(Row)
                    End If
                End If
            Else
                ' This wasn't a remove of a setting instance!
                Debug.Fail("Unknown component removed?")
            End If
        End Sub

        ''' <summary>
        ''' A component was added to our hosts container
        ''' </summary>
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        Private Sub ComponentAddedHandler(Sender As Object, e As ComponentEventArgs)
            If GetType(DesignTimeSettingInstance).IsAssignableFrom(CType(e.Component, Object).GetType()) Then
                ' A component was added - let's get the corresponding row from the grid...
                Dim Row As DataGridViewRow = RowFromComponent(DirectCast(e.Component, DesignTimeSettingInstance))

                If Row IsNot Nothing Then
                    ' This component was already showing...
                    Return
                Else
                    ' No row corresponding to this settings - we better add one!
                    Debug.Assert(_settingsGridView.RowCount >= 1)
                    ' We'll have to create a new row!
                    Dim NewRowIndex As Integer
                    NewRowIndex = _settingsGridView.Rows.Add()
                    ' Now, we don't want the last row, since that is the special "New row"
                    ' Let's grab the second last row...
                    Row = _settingsGridView.Rows(NewRowIndex)
                    Debug.Assert(NewRowIndex = _settingsGridView.RowCount - 2, "Why wasn't the new row added last?")
                    Row.Tag = e.Component
                End If
                SetUIRowValues(Row, DirectCast(e.Component, DesignTimeSettingInstance))
            Else
                ' This wasn't a remove of a setting instance!
                Debug.Fail("Unknown component type added!")
            End If
        End Sub

        ''' <summary>
        ''' Get a row from a component
        ''' </summary>
        ''' <param name="Instance"></param>
        ''' <remarks>O(n) running time</remarks>
        Private Function RowFromComponent(Instance As DesignTimeSettingInstance) As DataGridViewRow
            For Each Row As DataGridViewRow In _settingsGridView.Rows
                If Row.Tag Is Instance Then
                    Return Row
                End If
            Next
            Return Nothing
        End Function

        ''' <summary>
        ''' Get the instance from the current row, creating one if necessary
        ''' </summary>
        ''' <param name="Row"></param>
        Private Function ComponentFromRow(Row As DataGridViewRow) As DesignTimeSettingInstance
            If Row.Tag IsNot Nothing Then
                Debug.Assert(TypeOf Row.Tag Is DesignTimeSettingInstance, "Unknown tag of this object!")
                Return DirectCast(Row.Tag, DesignTimeSettingInstance)
            Else
                Dim NewInstance As New DesignTimeSettingInstance
                Row.Tag = NewInstance
                NewInstance.SetName(Row.Cells(NameColumnNo).FormattedValue.ToString())
                NewInstance.SetScope(CType(Row.Cells(ScopeColumnNo).Value, DesignTimeSettingInstance.SettingScope))
                If NewInstance.Name = "" Then
                    NewInstance.SetName(Settings.CreateUniqueName())
                End If
                Settings.Add(NewInstance)
                Return NewInstance
            End If
        End Function

        ''' <summary>
        ''' We can't allow commit of pending changes in some cases:
        ''' 1. We are showing an error dialog
        ''' 2. We are showing the type picker dialog
        ''' 3. We are showing a UI type editor
        ''' </summary>
        Private ReadOnly Property AllowCommitPendingChanges As Boolean
            Get
                If _isReportingError OrElse _isShowingTypePicker OrElse _inCellValidated OrElse _settingsGridView.CommittingChanges Then
                    Return False
                End If

                Dim ctrl As DataGridViewUITypeEditorEditingControl = TryCast(_settingsGridView.EditingControl, DataGridViewUITypeEditorEditingControl)
                If ctrl IsNot Nothing AndAlso ctrl.IsShowingUITypeEditor Then
                    Return False
                End If
                Return True
            End Get
        End Property

        ''' <summary>
        ''' The function forces to refresh the status of all commands.
        ''' </summary>
        Public Sub RefreshCommandStatus()
            If _menuCommands IsNot Nothing Then
                For Each command As DesignerMenuCommand In _menuCommands
                    command.RefreshStatus()
                Next
            End If
        End Sub

        ''' <summary>
        ''' Commit any pending changes
        ''' </summary>
        Public Function CommitPendingChanges(suppressValidationUI As Boolean, cancelOnValidationFailure As Boolean) As Boolean
            Dim savedSuppressValidationUI As Boolean = _suppressValidationUI
            Dim succeeded As Boolean = False
            Try
                _suppressValidationUI = suppressValidationUI
                If _settingsGridView.IsCurrentCellInEditMode Then
                    Debug.Assert(_settingsGridView.CurrentCell IsNot Nothing, "Grid in editing mode with no current cell???")
                    Try
                        If Not AllowCommitPendingChanges Then
                            succeeded = False
                        ElseIf ValidateCell(_settingsGridView.CurrentCell.EditedFormattedValue, _settingsGridView.CurrentCell.RowIndex, _settingsGridView.CurrentCell.ColumnIndex) Then
                            Dim oldSelectedCell As DataGridViewCell = _settingsGridView.CurrentCell
                            _settingsGridView.CurrentCell = Nothing
                            If oldSelectedCell IsNot Nothing Then
                                oldSelectedCell.Selected = True
                            End If
                            succeeded = True
                        Else
                            If cancelOnValidationFailure Then
                                _settingsGridView.CancelEdit()
                            End If
                        End If
                    Catch Ex As Exception
                        If _
                            Ex Is GetType(System.Threading.ThreadAbortException) OrElse
                            Ex Is GetType(StackOverflowException) Then
                            Throw
                        End If
                        Debug.Assert(Ex IsNot GetType(NullReferenceException) AndAlso Ex IsNot GetType(OutOfMemoryException),
                            String.Format("CommitPendingChanges caught exception {0}", Ex))
                    End Try
                Else
                    succeeded = True
                End If
            Finally
                _suppressValidationUI = savedSuppressValidationUI
            End Try

            ' If someone tells us to commit our pending changes, we have to make sure that the designer loader flushes.
            ' If we don't do this, the global settings object may come along and read stale data from the docdata's buffer...
            If DesignerLoader IsNot Nothing Then
                DesignerLoader.Flush()
            End If

            Return succeeded
        End Function

#Region "Column accessors"

        ''' <summary>
        ''' Type safe accessor for the type column
        ''' </summary>
        Private ReadOnly Property TypeColumn As DataGridViewComboBoxColumn
            Get
                Return DirectCast(_settingsGridView.Columns(TypeColumnNo), DataGridViewComboBoxColumn)
            End Get
        End Property

        ''' <summary>
        ''' Type safe accessor for the type column
        ''' </summary>
        Private ReadOnly Property ScopeColumn As DataGridViewComboBoxColumn
            Get
                Return DirectCast(_settingsGridView.Columns(ScopeColumnNo), DataGridViewComboBoxColumn)
            End Get
        End Property

#End Region

#Region "Private helper functions"

        ''' <summary>
        ''' Completely refresh grid (remove current rows and re-create them from settings
        ''' in associated designer
        ''' </summary>
        Private Sub RefreshGrid()
            If _settingsGridView.RowCount() > 0 Then
                _settingsGridView.Rows.Clear()
            End If

            For Each Instance As DesignTimeSettingInstance In Settings
                AddRow(Instance)
            Next
        End Sub

        ''' <summary>
        ''' Add a row to the grid, associate it with the the setting instance and update the UI
        ''' </summary>
        ''' <param name="Instance"></param>
        Private Sub AddRow(Instance As DesignTimeSettingInstance)
            If Not _settingsGridView.IsHandleCreated Then
                _settingsGridView.CreateControl()
            End If
            Debug.Assert(_settingsGridView.IsHandleCreated)
            Dim NewRowNo As Integer = _settingsGridView.Rows.Add()
            Dim NewRow As DataGridViewRow = _settingsGridView.Rows(NewRowNo)
            NewRow.Tag = Instance
            SetUIRowValues(NewRow, Instance)
        End Sub

        ''' <summary>
        ''' Make sure the user sees the properties as the are set in the setting instance
        ''' </summary>
        ''' <param name="Row"></param>
        Private Sub SetUIRowValues(Row As DataGridViewRow, Instance As DesignTimeSettingInstance)
            Row.Cells(NameColumnNo).Value = Instance.Name
            Row.Cells(NameColumnNo).ReadOnly = DesignTimeSettingInstance.IsNameReadOnly(Instance)

            ' Update type combobox, adding the instance's type if it isn't already included in the
            ' list
            Dim TypeCell As DataGridViewComboBoxCell = CType(Row.Cells(TypeColumnNo), DataGridViewComboBoxCell)
            Dim SettingTypeDisplayType As String = _typeNameResolver.PersistedSettingTypeNameToTypeDisplayName(Instance.SettingTypeName)
            If Not TypeColumn.Items.Contains(SettingTypeDisplayType) Then
                TypeColumn.Items.Insert(TypeColumn.Items.Count() - 1, SettingTypeDisplayType)
                SetComboBoxColumnDropdownWidth(TypeColumn)
            End If
            TypeCell.Value = SettingTypeDisplayType
            UpdateComboBoxCell(TypeCell)
            TypeCell.ReadOnly = DesignTimeSettingInstance.IsTypeReadOnly(Instance)

            Row.Cells(ScopeColumnNo).ReadOnly = DesignTimeSettingInstance.IsScopeReadOnly(Instance, _projectSystemSupportsUserScope)

            Row.Cells(ScopeColumnNo).Value = Instance.Scope

            UpdateUIValueColumn(Row)
        End Sub

        ''' <summary>
        ''' Update the value column of the current row and set the correct visual style
        ''' </summary>
        ''' <param name="row"></param>
        Private Sub UpdateUIValueColumn(row As DataGridViewRow)
            Dim Instance As DesignTimeSettingInstance = CType(row.Tag, DesignTimeSettingInstance)
            Dim Cell As DataGridViewUITypeEditorCell = CType(row.Cells(ValueColumnNo), DataGridViewUITypeEditorCell)
            If Instance IsNot Nothing Then
                Dim settingType As Type = _settingTypeCache.GetSettingType(Instance.SettingTypeName)
                If settingType IsNot Nothing AndAlso Not SettingTypeValidator.IsTypeObsolete(settingType) Then
                    Cell.ValueType = settingType
                    Cell.Value = _valueCache.GetValue(settingType, Instance.SerializedValue)
                Else
                    Cell.ValueType = GetType(String)
                    Cell.Value = Instance.SerializedValue
                End If

                Cell.ServiceProvider = Settings.Site
            Else
                ' If we don't have an instance for this row, the value should be an
                ' empty string
                Cell.ValueType = GetType(String)
                Cell.Value = ""
            End If
            _settingsGridView.InvalidateCell(Cell)
        End Sub

        ''' <summary>
        ''' Update the current cell to reflect the current value
        ''' </summary>
        ''' <param name="CellToUpdate"></param>
        ''' <remarks>The editing control isn't correctly updated if the cell changes "under" it</remarks>
        Private Sub UpdateComboBoxCell(CellToUpdate As DataGridViewComboBoxCell)
            If CellToUpdate Is _settingsGridView.CurrentCell AndAlso _settingsGridView.EditingControl IsNot Nothing Then
                If Not DBNull.Value.Equals(CellToUpdate.Value) Then
                    CellToUpdate.InitializeEditingControl(CellToUpdate.RowIndex, CellToUpdate.Value, CellToUpdate.Style)
                End If
            End If
        End Sub

        ''' <summary>
        ''' If files under source control, prompt the user if they want to check them out.
        ''' </summary>
        ''' <returns>True if not under source control or if the check out succeeded, false otherwise</returns>
        Private Function EnsureCheckedOut() As Boolean
            If DesignerLoader Is Nothing Then
                Debug.Fail("Failed to get the IDesignerLoaderService from out settings site (or the IDesignerLoaderService wasn't a SettingsDesignerLoader :(")
                Return False
            End If

            Try
                Return DesignerLoader.EnsureCheckedOut()
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(EnsureCheckedOut), NameOf(SettingsDesignerView))
                Throw
            End Try
        End Function

        Private Function IsDesignerEditable() As Boolean
            If DesignerLoader Is Nothing Then
                Debug.Fail("Failed to get the IDesignerLoaderService from out settings site (or the IDesignerLoaderService wasn't a SettingsDesignerLoader :(")
                Return False
            End If

            Return DesignerLoader.IsDesignerEditable
        End Function

#End Region

#Region "Selection service"

        Private _selectionServiceProperty As ISelectionService
        Private ReadOnly Property SelectionService As ISelectionService
            Get
                If _selectionServiceProperty Is Nothing Then
                    _selectionServiceProperty = DirectCast(GetService(GetType(ISelectionService)), ISelectionService)
                End If
                Return _selectionServiceProperty
            End Get
        End Property

        Private Sub OnSettingsGridViewCellStateChanged(sender As Object, e As DataGridViewCellStateChangedEventArgs) Handles _settingsGridView.CellStateChanged
            If SelectionService IsNot Nothing Then
                Dim SelectedComponents As New Hashtable()
                For Each cell As DataGridViewCell In _settingsGridView.SelectedCells
                    Dim Row As DataGridViewRow = _settingsGridView.Rows(cell.RowIndex)
                    If Row.Tag IsNot Nothing Then
                        SelectedComponents(Row.Tag) = True
                    End If
                Next

                SelectionService.SetSelectedComponents(SelectedComponents.Keys, SelectionTypes.Replace)
            End If
        End Sub

        Private Sub OnSettingsGridViewRowStateChanged(sender As Object, e As DataGridViewRowStateChangedEventArgs) Handles _settingsGridView.RowStateChanged
            If SelectionService IsNot Nothing Then
                Dim SelectedComponents As New Hashtable()

                For Each Row As DataGridViewRow In _settingsGridView.SelectedRows
                    If Row.Tag IsNot Nothing Then
                        SelectedComponents(Row.Tag) = True
                    End If
                Next
                SelectionService.SetSelectedComponents(SelectedComponents.Keys, SelectionTypes.Replace)
            End If
        End Sub

#End Region

#Region "Control event handlers"

        ''' <summary>
        ''' If multiple rows are selected, we need to wrap 'em all in a undo transaction...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewUserDeletingRow(sender As Object, e As DataGridViewRowCancelEventArgs) Handles _settingsGridView.UserDeletingRow
            If _settingsGridView.SelectedRows.Count = 0 AndAlso e.Row.IsNewRow Then
                ' The user cancelled an edit of the new row - we should not prevent the 
                ' datagridview from doing its magic and delete the new row...
                Return
            End If

            ' Make sure everything is checked out...
            If Not EnsureCheckedOut() Then
                e.Cancel = True
                Return
            End If

            ' We handle the delete explicitly here
            RemoveRows(_settingsGridView.SelectedRows)

            ' And cancel the "automatic" delete that is about to happen. The RemoveRows call should have 
            ' already taken care of this :)
            e.Cancel = True
        End Sub

        ''' <summary>
        ''' The user has deleted a row from the grid - let's make sure that we delete the corresponding
        ''' setting instance from the designed component...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewUserDeletedRow(sender As Object, e As DataGridViewRowEventArgs) Handles _settingsGridView.UserDeletedRow
            Dim InstanceToDelete As DesignTimeSettingInstance = CType(e.Row.Tag, DesignTimeSettingInstance)
            If InstanceToDelete IsNot Nothing Then
                Settings.Remove(InstanceToDelete)
            Else
                Debug.WriteLine("No Setting instance associated with deleted row!?")
            End If
        End Sub

        ''' <summary>
        ''' Helper method to validate a cell
        ''' </summary>
        ''' <param name="FormattedValue"></param>
        ''' <param name="RowIndex"></param>
        ''' <param name="ColumnIndex"></param>
        Private Function ValidateCell(FormattedValue As Object, RowIndex As Integer, ColumnIndex As Integer) As Boolean
            Dim Instance As DesignTimeSettingInstance = TryCast(_settingsGridView.Rows(RowIndex).Tag, DesignTimeSettingInstance)
            Select Case ColumnIndex
                Case NameColumnNo
                    Debug.Assert(TypeOf FormattedValue Is String, "Unknown type of formatted value for name")
                    Return ValidateName(DirectCast(FormattedValue, String), Instance)
                Case TypeColumnNo
                    ' We don't want to commit the "Browse..." value at any time... we also don't want to allow an empty string for type name
                    Debug.Assert(TypeOf FormattedValue Is String, "Unknown type of formatted value for name")
                    Return Not (TryCast(FormattedValue, String) = "" OrElse String.Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_BrowseType, TryCast(FormattedValue, String), StringComparison.Ordinal))
                Case ScopeColumnNo
                    Return TryCast(FormattedValue, String) <> ""
                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Validate cell contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewCellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs) Handles _settingsGridView.CellValidating
            ' We can get into this when delay-disposing due to project reloads...
            If Disposing Then Return

            If e.RowIndex = _settingsGridView.NewRowIndex Then
                ' Don't validate the new row...
                Return
            End If

            e.Cancel = Not ValidateCell(e.FormattedValue, e.RowIndex, e.ColumnIndex)
        End Sub

        Private Function ValidateName(NewName As String, Instance As DesignTimeSettingInstance) As Boolean
            ' If it was a valid name before, let's assume is still is :)
            If Instance IsNot Nothing AndAlso DesignTimeSettings.EqualIdentifiers(NewName, Instance.Name) Then
                Return True
            End If

            If NewName = "" Then
                If Not _suppressValidationUI Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_NameEmpty, HelpIDs.Err_NameBlank)
                End If
                Return False
            End If

            If Not Settings.IsUniqueName(NewName, IgnoreThisInstance:=Instance) Then
                ' There is already a setting with this name...
                If Not _suppressValidationUI Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_DuplicateName_1Arg, NewName), HelpIDs.Err_DuplicateName)
                End If
                Return False
            End If

            If Not Settings.IsValidName(NewName) Then
                If Not _suppressValidationUI Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidIdentifier_1Arg, NewName), HelpIDs.Err_InvalidName)
                End If
                Return False
            End If

            ' Everything is cool!
            Return True
        End Function

        ''' <summary>
        ''' Committing whatever change the user has done to the current cell
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewCellValidated(sender As Object, e As DataGridViewCellEventArgs) Handles _settingsGridView.CellValidated
            If Disposing Then
                Return
            End If

            If _inCellValidated Then
                Return
            End If
            Try
                _inCellValidated = True
                Dim Row As DataGridViewRow = _settingsGridView.Rows(e.RowIndex)

                Dim cell As DataGridViewCell = Row.Cells(e.ColumnIndex)
                Debug.Assert(cell IsNot Nothing, "Couldn't get current cell?")

                If Not _settingsGridView.IsCurrentRowDirty Then
                    ' This suxz, but since it seems that we get a validated event when 
                    ' the *current selected cell* changes, and not after *end edit*, we
                    ' check if the grid view thinks the current row is dirty!
                    ' CONSIDER: move this code to CellEndEdit event handler!
                    Return
                End If

                If Not EnsureCheckedOut() Then
                    Return
                End If

                Dim Instance As DesignTimeSettingInstance = ComponentFromRow(Row)
                Debug.Assert(Instance IsNot Nothing, "No DesignTimSetting associated with this row!?")

                Dim CellText As String = CStr(cell.EditedFormattedValue)

                '
                ' There is a slim, slim chance that the project will be reloaded when changing a property. 
                ' Currently, the only known time when that happens is when you rename a web reference typed setting
                ' (there is a corresponding property in the project file that will be set) but there is no way to 
                ' determine if anyone else is listening to ComponentChanged/ComponentChanging and doing something that
                ' will cause the project to be checked out. 
                ' 
                ' We'll take not of this fact by entering a protected ProjectCheckoutSection 
                '
                EnterProjectCheckoutSection()

                Try
                    Select Case e.ColumnIndex
                        Case NameColumnNo
                            Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing name of setting " & Instance.Name)
                            ' Don't use SetName since that won't fire a component change notification...
                            Instance.NameProperty.SetValue(Instance, CellText)
                        Case TypeColumnNo
                            ' Changing the type is a remove/add operation
                            If Not CellText.Equals(Instance.SettingTypeName, StringComparison.Ordinal) AndAlso Not CellText.Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_BrowseType, StringComparison.Ordinal) Then
                                ChangeSettingType(Row, CellText)
                            End If
                        Case ScopeColumnNo
                            Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing scope of setting " & Instance.Name)
                            Instance.ScopeProperty.SetValue(Instance, cell.Value)
                        Case ValueColumnNo
                            ' It seems that we get a cell validated event even if we haven't edited the text in the cell....
                            If Not String.Equals(Instance.SerializedValue, CellText, StringComparison.Ordinal) Then
                                ' We only set the value in if the text in the validated cell
                                ' is different from the previous value
                                Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing value of setting " & Instance.Name)
                                Instance.SerializedValueProperty.SetValue(Instance, SettingsValueSerializer.Serialize(cell.Value, CultureInfo.InvariantCulture))
                            End If
                    End Select
                Catch ex As Exception
                    ' We only "expect" checkout exceptions, but we may want to know about other exceptions as well...
                    Debug.Assert(TypeOf ex Is CheckoutException, String.Format("Unknown exception {0} caught while changing property", ex))

                    ' Try & tell the user that something went wrong...
                    If Not ProjectReloadedDuringCheckout Then
                        If Settings IsNot Nothing AndAlso Settings.Site IsNot Nothing Then
                            DesignerMessageBox.Show(Settings.Site, "", ex, DesignUtil.GetDefaultCaption(Settings.Site))
                        End If
                        ' And make sure that the UI reflects the actual values of the corresponding setting...
                        SetUIRowValues(Row, Instance)
                    End If
                Finally
                    LeaveProjectCheckoutSection()
                End Try
            Finally
                _inCellValidated = False
            End Try
        End Sub

        Private Sub OnSettingsGridViewCellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles _settingsGridView.CellFormatting
            ' If the column is the Scope column, check the
            ' value.
            If e.ColumnIndex = ScopeColumnNo Then
                If Not DBNull.Value.Equals(e.Value) AndAlso e.Value IsNot Nothing Then
                    Dim row As DataGridViewRow = _settingsGridView.Rows(e.RowIndex)
                    Dim instance As DesignTimeSettingInstance = TryCast(row.Tag, DesignTimeSettingInstance)
                    e.Value = DesignTimeSettingInstance.ScopeConverter.ConvertToLocalizedString(instance, CType(e.Value, DesignTimeSettingInstance.SettingScope))
                    e.FormattingApplied = True
                End If
            End If
        End Sub

        Private Sub OnSettingsGridViewOnEditingControlShowing(sender As Object, e As DataGridViewEditingControlShowingEventArgs) Handles _settingsGridView.EditingControlShowing
            ' Work-around for VsWhidbey 228617
            Dim tb As TextBox = TryCast(e.Control, TextBox)
            If tb IsNot Nothing Then
                tb.Multiline = False
                tb.AcceptsReturn = False
            End If
        End Sub

        ''' <summary>
        ''' We want to prevent us from going into edit mode on the first click...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewOnCellClickBeginEdit(sender As Object, e As CancelEventArgs) Handles _settingsGridView.CellClickBeginEdit
            If DesignerLoader IsNot Nothing Then
                e.Cancel = Not DesignerLoader.OkToEdit()
            End If
        End Sub

        ''' <summary>
        ''' The user has started editing a cell. We've gotta make sure that:
        '''  1. The file is checked out if under source control
        '''  2. The cell style in the Value column is "Default"
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewCellBeginEdit(sender As Object, e As DataGridViewCellCancelEventArgs) Handles _settingsGridView.CellBeginEdit
            ' Check out , but we can't check out if settings file is readonly
            If Not EnsureCheckedOut() Then
                e.Cancel = True
            Else
                Select Case e.ColumnIndex
                    Case NameColumnNo
                        '
                        ' If this is the name of a web reference setting, we need to check out the project file since the name of
                        ' the setting that contains the web service URL is stored in the project file.
                        ' In a perfect world, the disco code generator (or even the IVsRefactorNotify 
                        If e.RowIndex <> _settingsGridView.NewRowIndex Then
                            Dim instance As DesignTimeSettingInstance = ComponentFromRow(_settingsGridView.Rows(e.RowIndex))
                            If instance IsNot Nothing AndAlso instance.SettingTypeName.Equals(SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                                If DesignerLoader IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem.ContainingProject IsNot Nothing _
                                    AndAlso DesignerLoader.ProjectItem.ContainingProject.FullName <> "" _
                                    AndAlso Settings IsNot Nothing _
                                    AndAlso Settings.Site IsNot Nothing _
                                Then
                                    Dim files As New List(Of String) From {
                                        DesignerLoader.ProjectItem.ContainingProject.FullName
                                    }
                                    If Not SourceCodeControlManager.QueryEditableFiles(Settings.Site, files, False, False) Then
                                        e.Cancel = True
                                    End If
                                End If
                            End If
                        End If
                    Case ValueColumnNo
                        Dim cell As DataGridViewUITypeEditorCell = TryCast(
                                        _settingsGridView.Rows(e.RowIndex).Cells(e.ColumnIndex),
                                        DataGridViewUITypeEditorCell)

                        ' If the type has been invalidated, we need to make sure that we treat it as a string...
                        ' We know that our internal serializable connection strings can never be invalidated, so we don't 
                        ' need to check for invalidated connection string types...
                        If cell IsNot Nothing AndAlso cell.ValueType IsNot Nothing AndAlso cell.ValueType IsNot GetType(SerializableConnectionString) Then
                            Dim reresolvedSettingType As Type = _settingTypeCache.GetSettingType(cell.ValueType.FullName)

                            If reresolvedSettingType IsNot Nothing Then
                                If SettingTypeValidator.IsTypeObsolete(reresolvedSettingType) Then
                                    Dim formattedValue As Object = cell.FormattedValue
                                    cell.ValueType = GetType(String)
                                    cell.Value = formattedValue
                                End If
                            End If
                        End If
                End Select
            End If
        End Sub

        ''' <summary>
        ''' Get access to a UI service - useful to pop up message boxes and getting fonts
        ''' </summary>
        Private ReadOnly Property UIService As IUIService
            Get
                Dim Result As IUIService
                Result = CType(GetService(GetType(IUIService)), IUIService)

                Debug.Assert(Result IsNot Nothing, "Failed to get IUIService")
                Return Result
            End Get
        End Property

        ''' <summary>
        ''' The user has added a row to the grid
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Shouldn't have to do this, but it seems that the tag property of the new row is copied from the previous row</remarks>
        Private Sub OnSettingsGridViewUserAddedRow(sender As Object, e As DataGridViewRowEventArgs) Handles _settingsGridView.UserAddedRow
            e.Row.Tag = Nothing
        End Sub

        ''' <summary>
        ''' We've gotta fill out the default values for the new row!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewDefaultValuesNeeded(sender As Object, e As DataGridViewRowEventArgs) Handles _settingsGridView.DefaultValuesNeeded
            Dim SampleInstance As New DesignTimeSettingInstance()
            If Not _projectSystemSupportsUserScope Then
                SampleInstance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
            End If
            SampleInstance.SetName(Settings.CreateUniqueName())
            SetUIRowValues(e.Row, SampleInstance)
        End Sub

        ''' <summary>
        ''' Change the type of the setting on the current row
        ''' </summary>
        ''' <param name="Row"></param>
        Private Sub ChangeSettingType(Row As DataGridViewRow, TypeDisplayName As String)
            Dim addingNewSetting As Boolean = Row.Tag Is Nothing

            ' Get the current setting instance.
            Dim Instance As DesignTimeSettingInstance = ComponentFromRow(Row)

            ' Let's get the display name for the new type... 
            Dim newTypeName As String = _typeNameResolver.TypeDisplayNameToPersistedSettingTypeName(TypeDisplayName)
            Dim newType As Type = _settingTypeCache.GetSettingType(newTypeName)

            ' Only change type of this setting if the display name of the types are different...
            If addingNewSetting OrElse Not String.Equals(Instance.SettingTypeName, newTypeName, StringComparison.Ordinal) Then
                Using Transaction As New SettingsDesignerUndoTransaction(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_TypeChanged)
                    Debug.WriteLineIf(SettingsDesigner.TraceSwitch.TraceVerbose, "Changing type of setting " & Instance.Name)
                    Instance.TypeNameProperty.SetValue(Instance, newTypeName)
                    If newType IsNot Nothing Then
                        Dim newValue As Object = _valueCache.GetValue(newType, Instance.SerializedValue)

                        ' If we don't have a value, and the new type is a value type, we want to 
                        ' give an "empty" default value for the value type to avoid run time type
                        ' cast exceptions in the users code for C# (DevDiv 24835)
                        If newValue Is Nothing AndAlso GetType(ValueType).IsAssignableFrom(newType) Then
                            Try
                                newValue = Activator.CreateInstance(newType)
                            Catch ex As Exception
                                ' We gave it a shot... but unfortunately, we didn't succeed...
                                ' It is now up to the user to specify an appropriate default value
                            End Try
                        End If
                        Instance.SerializedValueProperty.SetValue(Instance, SettingsValueSerializer.Serialize(newValue, CultureInfo.InvariantCulture))
                    End If

                    ' If we changed the type to a connection string, we should also make sure that the scope is application...
                    If newType Is GetType(SerializableConnectionString) AndAlso Instance.Scope <> DesignTimeSettingInstance.SettingScope.Application Then
                        Instance.ScopeProperty.SetValue(Instance, DesignTimeSettingInstance.SettingScope.Application)
                    End If
                    Transaction.Commit()
                End Using

                If newType IsNot Nothing AndAlso
                   _settingTypeCache.IsWellKnownType(newType) Then
                    '
                    ' Try to add a reference to the type (if not already in the project)
                    '
                    Try
                        If DesignerLoader IsNot Nothing Then
                            Dim dteProj As EnvDTE.Project = DTEUtils.EnvDTEProject(DesignerLoader.VsHierarchy)
                            Dim vsLangProj As VSLangProj.VSProject = Nothing
                            If dteProj IsNot Nothing Then
                                vsLangProj = TryCast(dteProj.Object, VSLangProj.VSProject)
                            End If

                            If vsLangProj IsNot Nothing AndAlso vsLangProj.References.Find(newType.Assembly.GetName().Name) Is Nothing Then
                                vsLangProj.References.Add(newType.Assembly.GetName().Name)
                            End If
                        End If
                    Catch ex As CheckoutException
                        'Ignore CheckoutException
                    Catch ex As Exception When ReportWithoutCrash(ex, "Failed to add reference to assembly contining type", NameOf(SettingsDesignerView))
                        ' Well, we mostly tried to be nice to the user and automatically add the reference here... 
                        ' If we fail, the user will see an annoying error about undefined types, but it shouldn't be the
                        ' end of the world...
                    End Try
                End If
            End If

            ' We always need to update the UI, since the user may have selected the same type as the setting already was
            ' in the type browser dialog, and not updating here would keep the dialog showing "browse..."
            SetUIRowValues(Row, ComponentFromRow(Row))
        End Sub

        ''' <summary>
        ''' To find out when the user clicks on the "browse..." item in the types combo box,
        ''' we have got to "commit" (end edit) the value every time the cell gets dirty!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Kind of hacky...</remarks>
        Private Sub OnSettingsGridViewCurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles _settingsGridView.CurrentCellDirtyStateChanged
            If _settingsGridView.CurrentCellAddress.X = TypeColumnNo Then
                Dim cell As DataGridViewCell = _settingsGridView.CurrentCell
                If cell IsNot Nothing Then
                    If My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_BrowseType.Equals(cell.EditedFormattedValue) Then
                        TypeComboBoxSelectedIndexChanged()
                    ElseIf TryCast(cell.EditedFormattedValue, String) = "" Then
                        _settingsGridView.CancelEdit()
                    Else
                        ' If we don't have a setting associated with the current row, we force create one
                        ' by getting the component from the row (if we don't do this, there won't be an undo
                        ' unit for this - the settings won't be created until we leave the cell)
                        Dim row As DataGridViewRow = _settingsGridView.Rows(_settingsGridView.CurrentCellAddress.Y)
                        If row IsNot Nothing Then
                            ComponentFromRow(row)
                        End If

                        _settingsGridView.CommitEdit(DataGridViewDataErrorContexts.Commit)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whenever the datagridview finds something to complain about, it will call the DataError 
        ''' event handler
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnSettingsGridViewOnDataError(sender As Object, e As DataGridViewDataErrorEventArgs) Handles _settingsGridView.DataError
            If Not _suppressValidationUI Then
                Select Case e.ColumnIndex
                    Case ValueColumnNo
                        ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidValue_2Arg, _settingsGridView.CurrentCell.GetEditedFormattedValue(e.RowIndex, DataGridViewDataErrorContexts.Display), _settingsGridView.Rows(_settingsGridView.CurrentCell.RowIndex).Cells(TypeColumnNo).FormattedValue), HelpIDs.Err_FormatValue)
                    Case NameColumnNo
                        ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidIdentifier_1Arg, _settingsGridView.CurrentCell.GetEditedFormattedValue(e.RowIndex, DataGridViewDataErrorContexts.Display)), HelpIDs.Err_InvalidName)
                    Case Else
                        ' For some reason, we get data errors when we don't have a value for a specific row 
                        ' (i.e. not set type or scope). We'll just ignore these for now...
                End Select
            End If
            e.Cancel = True
        End Sub

#End Region

        ''' <summary>
        ''' The user wants to view (and maybe add) code that extends the generated settings class
        ''' </summary>
        Private Sub ViewCode()
            Dim Hierarchy As IVsHierarchy = DirectCast(Settings.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Dim ProjectItem As EnvDTE.ProjectItem = DirectCast(Settings.Site.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
            Dim VSMDCodeDomProvider As IVSMDCodeDomProvider = DirectCast(Settings.Site.GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
            If Hierarchy Is Nothing OrElse ProjectItem Is Nothing OrElse VSMDCodeDomProvider Is Nothing Then
                ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGEN_FAILEDOPENCREATEEXTENDINGFILE, HelpIDs.Err_ViewCode)
            Else
                Try
                    If ProjectItem.ProjectItems Is Nothing OrElse ProjectItem.ProjectItems.Count = 0 Then
                        ' If we don't have any subitems, we better try & run the custom tool...
                        Dim vsProjectItem As VSLangProj.VSProjectItem = TryCast(ProjectItem.Object, VSLangProj.VSProjectItem)
                        If vsProjectItem IsNot Nothing Then
                            vsProjectItem.RunCustomTool()
                        End If
                    End If
                    Dim FullyQualifiedClassName As String = SettingsDesigner.FullyQualifiedGeneratedTypedSettingsClassName(Hierarchy, VSITEMID.NIL, Settings, ProjectItem)
                    Dim suggestedFileName As String = ""
                    If Settings.UseSpecialClassName AndAlso IsVbProject(Hierarchy) AndAlso SettingsDesigner.IsDefaultSettingsFile(Hierarchy, DesignerLoader.ProjectItemid) Then
                        suggestedFileName = "Settings"
                    End If
                    ProjectUtils.OpenAndMaybeAddExtendingFile(FullyQualifiedClassName, suggestedFileName, Settings.Site, Hierarchy, ProjectItem, CType(VSMDCodeDomProvider.CodeDomProvider, CodeDom.Compiler.CodeDomProvider), Me)
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ViewCode), NameOf(SettingsDesignerView))
                    If Settings IsNot Nothing AndAlso Settings.Site IsNot Nothing Then
                        ' We better tell the user that something went wrong (if we still have a settings/settings.site that is)
                        DesignerMessageBox.Show(Settings.Site, ex, DesignUtil.GetDefaultCaption(Settings.Site))
                    End If
                End Try
            End If
        End Sub

        Private Sub RemoveRows(rowsToDelete As ICollection)
            Dim undoTran As SettingsDesignerUndoTransaction = Nothing
            Try
                If rowsToDelete.Count > 1 Then
                    ' If there is more than one row to delete, we need to wrap this in an undo transaction...
                    undoTran = New SettingsDesignerUndoTransaction(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_RemoveMultipleSettings_1Arg, rowsToDelete.Count))
                End If
                For Each row As DataGridViewRow In rowsToDelete
                    If row.Tag IsNot Nothing Then
                        ' Removing the setting will fire a ComponentRemoved, which
                        ' will remove the row from the grid...
                        Settings.Remove(DirectCast(row.Tag, DesignTimeSettingInstance))
                    End If
                Next
                If undoTran IsNot Nothing Then
                    ' Commit undo transaction (if any)
                    undoTran.Commit()
                End If
            Finally
                If undoTran IsNot Nothing Then
                    undoTran.Dispose()
                End If
            End Try

        End Sub

#Region "Context menus"

        ''' <summary>
        ''' Remove event handler for showing context menu
        ''' </summary>
        ''' <param name="Designer"></param>
        Private Sub UnregisterMenuCommands(Designer As SettingsDesigner)
            RemoveHandler _settingsGridView.ContextMenuShow, AddressOf Designer.ShowContextMenu
            RemoveHandler _settingsGridView.KeyDown, AddressOf OnGridKeyDown
        End Sub

        ''' <summary>
        ''' Register the settings designer menu commands(context menus)
        ''' </summary>
        Private Sub RegisterMenuCommands(Designer As SettingsDesigner)
            'Protect against recursively invoking this
            Static InThisMethod As Boolean
            If InThisMethod Then
                Debug.Fail("RegisterMenuCommands was invoked recursively")
                Exit Sub
            End If

            InThisMethod = True
            Try
                _menuCommands = New ArrayList From {
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONEditCell, AddressOf MenuEditCell, AddressOf MenuEditCellEnableHandler,
                    AlwaysCheckStatus:=True),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONAddRow, AddressOf MenuAddSetting, AddressOf MenuAddSettingEnableHandler, CommandText:=My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_MNU_AddSettingText),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDCOMMONRemoveRow, AddressOf MenuRemove, AddressOf MenuRemoveEnableHandler,
                    AlwaysCheckStatus:=True, CommandText:=My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_MNU_RemoveSettingText),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerViewCode, AddressOf MenuViewCode, AddressOf MenuViewCodeEnableHandler),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerSynchronize, AddressOf MenuSynchronizeUserConfig, AddressOf MenuSynchronizeUserConfigEnableHandler),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDSettingsDesignerLoadWebSettings, AddressOf MenuLoadWebSettingsFromAppConfig, AddressOf MenuLoadWebSettingsFromAppConfigEnableHandler, AlwaysCheckStatus:=True),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd2kECMD_CANCEL, AddressOf MenuCancelEdit, AddressOf MenuCancelEditEnableHandler),
                    New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd97cmdidViewCode, AddressOf MenuViewCode, AddressOf MenuViewCodeEnableHandler)
                }
                'Delete
                '
                'We don't actually have a Delete command (the AddressOf MenuRemove is a dummy, since DesignerMenuCommand wants something
                '  for this argumnet).
                'We only have this command here because we need to be able to make the "Delete" command in the main menu hidden.  We
                '  use Remove instead of Delete.
                Dim DeleteCommand As DesignerMenuCommand = New DesignerMenuCommand(Designer, Constants.MenuConstants.CommandIDVSStd97cmdidDelete, AddressOf MenuRemove)
                _menuCommands.Add(DeleteCommand)
                '
                '... So, make Edit.Delete in Devenv's menus invisible always for our editor.
                DeleteCommand.Visible = False
                DeleteCommand.Enabled = False

                'Add the "Access modifier" combobox menu commands
                _menuCommands.AddRange(_accessModifierCombobox.GetMenuCommandsToRegister())

                Designer.RegisterMenuCommands(_menuCommands)

                AddHandler _settingsGridView.ContextMenuShow, AddressOf Designer.ShowContextMenu
            Finally
                InThisMethod = False
            End Try
        End Sub

        Friend Sub OnDesignerWindowActivated(activated As Boolean)
            _accessModifierCombobox.OnDesignerWindowActivated(activated)
            If activated Then
                UpdateToolbarFocus()
            End If
        End Sub

        ''' <summary>
        ''' Tell the shell that our toolbar wants to be included in the translation of
        ''' accelerators/alt-shift navigation
        ''' </summary>
        Private Sub UpdateToolbarFocus()
            If _toolbarPanel IsNot Nothing Then
                _toolbarPanel.Activate(Handle)
            End If
        End Sub

        ''' <summary>
        ''' The Cancel Edit command is never enabled. 
        ''' </summary>
        ''' <param name="menucommand">Ignored</param>
        ''' <returns>False</returns>
        ''' <remarks>
        ''' We never enable this command because we are currently trying to commit all pending edits in our 
        ''' IVsWindowPaneCommit_CommitPendingEdit implementation, which means that we'll try to commit the broken cell before
        ''' our command handler will be executed. By registering this command with the ESC keybinding, and always disable it,
        ''' we basically unbind the keyboard shortcut and let the DataGridView do it's built-in thing (which happens to be the 
        ''' right thing :)            
        ''' </remarks>
        Private Function MenuCancelEditEnableHandler(menucommand As DesignerMenuCommand) As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Should the remove menu item be enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        Private Function MenuRemoveEnableHandler(MenuCommand As DesignerMenuCommand) As Boolean
            ' If we are not editable we shouldn't allow removal of rows...
            If Not IsDesignerEditable() Then
                Return False
            End If

            ' If we are currently in edit mode, we can't allow users to remove rows, since that may
            ' prove problematic if the current row is invalid (we don't 
            If _settingsGridView.IsCurrentCellInEditMode Then
                Return False
            End If

            For Each cell As DataGridViewCell In _settingsGridView.SelectedCells
                If cell.RowIndex <> _settingsGridView.NewRowIndex Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Is the EditCell command enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        Private Function MenuEditCellEnableHandler(MenuCommand As DesignerMenuCommand) As Boolean
            Return _settingsGridView.CurrentCell IsNot Nothing AndAlso Not _settingsGridView.IsCurrentCellInEditMode AndAlso IsDesignerEditable() AndAlso Not _settingsGridView.CurrentCell.ReadOnly
        End Function

        ''' <summary>
        ''' Indicate if the view code button should be enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        Private Function MenuViewCodeEnableHandler(MenuCommand As DesignerMenuCommand) As Boolean
            If Not IsDesignerEditable() Then
                Return False
            End If

            If _cachedCodeProvider Is Nothing Then
                ' Let's see if we support partial classes?
                '
                Dim VSMDCodeDomProvider As IVSMDCodeDomProvider =
                            DirectCast(GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                If VSMDCodeDomProvider IsNot Nothing Then
                    _cachedCodeProvider = TryCast(VSMDCodeDomProvider.CodeDomProvider, CodeDom.Compiler.CodeDomProvider)
                    _viewCodeEnabled = _cachedCodeProvider IsNot Nothing AndAlso _cachedCodeProvider.Supports(CodeDom.Compiler.GeneratorSupport.PartialTypes)
                End If
            End If
            Return _viewCodeEnabled
        End Function

        ''' <summary>
        ''' Cancel the current edit
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>See MenuCancelEditEnableHandler as to why this should never be enabled</remarks>
        Private Sub MenuCancelEdit(sender As Object, e As EventArgs)
            Debug.Fail("We should never enable the CancelEdit command - we should let the datagrid do it's work!")
        End Sub

        ''' <summary>
        ''' View the "code-beside" file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuViewCode(sender As Object, e As EventArgs)
            ViewCode()
        End Sub

        ''' <summary>
        ''' Is the Synchronize command on the settings designer toolbar enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        Private Function MenuSynchronizeUserConfigEnableHandler(MenuCommand As DesignerMenuCommand) As Boolean
            If Not IsDesignerEditable() Then
                Return False
            End If

            If _hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = DTEUtils.EnvDTEProject(_hierarchy)
                Return proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing
            End If
            Return False
        End Function

        ''' <summary>
        ''' Is the Load Web Settings command on the settings designer toolbar enabled?
        ''' </summary>
        ''' <param name="MenuCommand"></param>
        Private Function MenuLoadWebSettingsFromAppConfigEnableHandler(MenuCommand As DesignerMenuCommand) As Boolean
            If Not IsDesignerEditable() Then
                Return False
            End If

            If _hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = DTEUtils.EnvDTEProject(_hierarchy)
                If proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing _
                    AndAlso Settings IsNot Nothing _
                    AndAlso Settings.Site IsNot Nothing Then
                    Try
                        Dim doc As XmlDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(CType(Settings.Site, IServiceProvider), _hierarchy, False)

                        ' DevDiv Bugs 198406
                        ' If the application is targetting .Net 3.5 SP1, client subset, then disable "Load Web Settings" menu button because only a subset 
                        ' of the Full .Net Framework assemblies will be available to this application, in particular the client 
                        ' subset will NOT include System.Web.Extensions.dll

                        If IsClientFrameworkSubset(_hierarchy) Then
                            Return False
                        End If

                        Return Not String.IsNullOrEmpty(ServicesPropPageAppConfigHelper.WebSettingsHost(doc))
                    Catch ex As XmlException
                        'The xml's bad: just disable the button
                    End Try
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Delete any and all user.config files...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuSynchronizeUserConfig(sender As Object, e As EventArgs)
            Dim allDeletedFiles As New List(Of String)
            Dim allDeletedDirectories As New List(Of String)

            If DesignerLoader IsNot Nothing AndAlso DesignerLoader.VsHierarchy IsNot Nothing Then
                Dim configDirs As List(Of String) = Nothing
                Dim filesToDelete As List(Of String) = Nothing

                Try
                    configDirs = SettingsDesigner.FindUserConfigDirectories(DesignerLoader.VsHierarchy)
                    filesToDelete = SettingsDesigner.FindUserConfigFiles(configDirs)
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(MenuSynchronizeUserConfig), NameOf(SettingsDesignerView))
                End Try

                If filesToDelete Is Nothing OrElse filesToDelete.Count = 0 Then
                    ' Couldn't find any files to delete - let's tell the user...
                    If configDirs Is Nothing Then
                        configDirs = New List(Of String)
                    End If
                    Dim dirs As String = String.Join(vbNewLine, configDirs.ToArray())
                    DesignerMessageBox.Show(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_SyncFilesNoFilesFound_1Arg, dirs), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Information, HelpLink:=HelpIDs.Err_SynchronizeUserConfig)
                Else
                    Dim fileList As String
                    Const FilesToShow As Integer = 15
                    fileList = String.Join(vbNewLine, filesToDelete.ToArray(), 0, Math.Min(FilesToShow, filesToDelete.Count))
                    If filesToDelete.Count > FilesToShow Then
                        fileList = fileList & vbNewLine & "..."
                    End If

                    If DesignerMessageBox.Show(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_SyncFiles_1Arg, fileList), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OKCancel, MessageBoxIcon.Information, HelpLink:=HelpIDs.Err_SynchronizeUserConfig) = DialogResult.OK Then
                        If Not SettingsDesigner.DeleteFilesAndDirectories(filesToDelete, Nothing) Then
                            DesignerMessageBox.Show(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_SyncFilesOneOrMoreFailed, DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Warning, HelpLink:=HelpIDs.Err_SynchronizeUserConfig)
                        End If
                    End If
                End If
            End If

        End Sub

        ''' <summary>
        ''' Load web settings from app.config file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuLoadWebSettingsFromAppConfig(sender As Object, e As EventArgs)

            If Not IsDesignerEditable() Then
                Return
            End If

            If _hierarchy IsNot Nothing Then
                Dim proj As EnvDTE.Project = DTEUtils.EnvDTEProject(_hierarchy)
                If proj IsNot Nothing _
                    AndAlso proj.ConfigurationManager IsNot Nothing _
                    AndAlso Settings IsNot Nothing _
                    AndAlso Settings.Site IsNot Nothing Then

                    Try
                        Dim doc As XmlDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(CType(Settings.Site, IServiceProvider), _hierarchy, False)
                        If doc IsNot Nothing Then
                            Dim authenticationUrl As String = ServicesPropPageAppConfigHelper.AuthenticationServiceUrl(doc)
                            Dim authenticationHost As String = ServicesPropPageAppConfigHelper.AuthenticationServiceHost(doc)
                            Using servicesAuthForm As New ServicesAuthenticationForm(authenticationUrl, authenticationHost, CType(Settings.Site, IServiceProvider))
                                If ServicesPropPageAppConfigHelper.WindowsAuthSelected(doc) Then
                                    'DevDiv Bugs 121204, according the manuva, Windows Auth always happens
                                    'automatically so we can just treat this case like anonymous and skip going
                                    'to the Auth Service
                                    servicesAuthForm.LoadAnonymously = True
                                Else
                                    Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                                        Dim result As DialogResult = servicesAuthForm.ShowDialog()
                                        If result = DialogResult.Cancel Then Exit Sub
                                    End Using
                                    'TODO: What exceptions do we need to catch?
                                End If
                                If servicesAuthForm.LoadAnonymously OrElse
                                    (Not servicesAuthForm.LoadAnonymously AndAlso ClientFormsAuthenticationMembershipProvider.ValidateUser(servicesAuthForm.UserName, servicesAuthForm.Password, servicesAuthForm.AuthenticationUrl)) Then
                                    Dim webSettingsUrl As String = ServicesPropPageAppConfigHelper.WebSettingsUrl(doc)
                                    Dim Collection As SettingsPropertyCollection = ClientSettingsProvider.GetPropertyMetadata(webSettingsUrl)

                                    Dim badNames As New List(Of String)
                                    Dim unreferencedTypes As New List(Of String)
                                    Using Transaction As New SettingsDesignerUndoTransaction(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_TypeChanged)
                                        RemoveAllWebProviderSettings()

                                        For Each settingsProp As SettingsProperty In Collection
                                            If (Not servicesAuthForm.LoadAnonymously) OrElse AllowsAnonymous(settingsProp) Then
                                                If Not Settings.IsUniqueName(settingsProp.Name) Then
                                                    badNames.Add(settingsProp.Name)
                                                Else
                                                    Dim newInstance As New DesignTimeSettingInstance
                                                    If settingsProp.PropertyType Is Nothing Then
                                                        unreferencedTypes.Add(settingsProp.Name)
                                                    Else
                                                        newInstance.SetName(settingsProp.Name)
                                                        newInstance.SetSettingTypeName(settingsProp.PropertyType.FullName)
                                                        newInstance.SetProvider(ServicesPropPageAppConfigHelper.ClientSettingsProviderName)
                                                        'TODO: Is this the right string value
                                                        If settingsProp.DefaultValue IsNot Nothing Then
                                                            newInstance.SetSerializedValue(settingsProp.DefaultValue.ToString())
                                                        End If
                                                        If settingsProp.IsReadOnly Then
                                                            newInstance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                                                        End If
                                                        Settings.Add(newInstance)
                                                    End If
                                                End If
                                            End If
                                        Next
                                        Transaction.Commit()
                                    End Using

                                    ShowErrorIfThereAreUnreferencedTypes(unreferencedTypes)
                                    ShowErrorIfThereAreDuplicateNames(badNames)

                                    'TODO: This doesn't seem to be public, and http://ddindex is down...
                                    'Catch actionNotSupported As System.ServiceModel.ActionNotSupportedException
                                    '    DesignerFramework.DesignerMessageBox.Show(Me.Settings.Site, "", actionNotSupported, DesignerFramework.DesignUtil.GetDefaultCaption(Settings.Site))
                                Else
                                    DesignerMessageBox.Show(Settings.Site, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_CantAuthenticate, DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                End If
                            End Using
                        End If
                    Catch innerException As XmlException
                        Dim ex As New XmlException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_InvalidAppConfigXml)
                        DesignerMessageBox.Show(Settings.Site, "", ex, DesignUtil.GetDefaultCaption(Settings.Site))
                    End Try
                End If
            End If
        End Sub

        ''' <summary>
        ''' Whether this SettingsProperty allows anonymous access
        ''' </summary>
        ''' <param name="settingsProp">The SettingsProperty to check for AllowsAnonymous attribute</param>
        Private Shared Function AllowsAnonymous(settingsProp As SettingsProperty) As Boolean
            If settingsProp IsNot Nothing AndAlso settingsProp.Attributes IsNot Nothing AndAlso
            settingsProp.Attributes.ContainsKey("AllowAnonymous") Then
                Dim value As Object = settingsProp.Attributes("AllowAnonymous")
                Return value IsNot Nothing AndAlso value.Equals(True)
            End If
            Return False
        End Function

        ''' <summary>
        ''' If there are unreferenced types, display an error dialog
        ''' </summary>
        ''' <param name="badNames">List of the bad names</param>
        Private Sub ShowErrorIfThereAreUnreferencedTypes(badNames As List(Of String))
            If badNames.Count > 0 Then
                Dim displayString As String = String.Join(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator, badNames.ToArray())
                DesignerMessageBox.Show(Settings.Site, String.Format(CultureInfo.CurrentCulture, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_UnreferencedTypeNameList_1Arg, displayString), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        ''' <summary>
        ''' If there are duplicate names, display an error dialog
        ''' </summary>
        ''' <param name="badNames">List of the bad names</param>
        Private Sub ShowErrorIfThereAreDuplicateNames(badNames As List(Of String))
            If badNames.Count > 0 Then
                Dim displayString As String = String.Join(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ListSeparator, badNames.ToArray())
                DesignerMessageBox.Show(Settings.Site, String.Format(CultureInfo.CurrentCulture, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_DuplicateNameList_1Arg, displayString), DesignUtil.GetDefaultCaption(Settings.Site), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        ''' <summary>
        ''' Remove all the WebProvider settings
        ''' </summary>
        Private Sub RemoveAllWebProviderSettings()
            Dim settingsToRemove As New List(Of DesignTimeSettingInstance)
            Dim setting As DesignTimeSettingInstance

            For Each setting In Settings
                If DesignTimeSettingInstance.IsWebProvider(setting) Then
                    settingsToRemove.Add(setting)
                End If
            Next

            For Each setting In settingsToRemove
                Settings.Remove(setting)
            Next
        End Sub

        ''' <summary>
        ''' Should the Add setting menu command be enabled?
        ''' </summary>
        ''' <param name="menucommand"></param>
        Private Function MenuAddSettingEnableHandler(menucommand As DesignerMenuCommand) As Boolean
            Return IsDesignerEditable()
        End Function

        ''' <summary>
        ''' Add a new setting to the grid
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuAddSetting(sender As Object, e As EventArgs)
            _settingsGridView.CurrentCell = _settingsGridView.Rows(_settingsGridView.Rows.Count - 1).Cells(NameColumnNo)
            Debug.Assert(_settingsGridView.CurrentRow.Tag Is Nothing, "Adding a new setting failed - there is already a setting associated with the new row!?")
            _settingsGridView.BeginEdit(True)
        End Sub

        ''' <summary>
        ''' Start editing the current cell
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuEditCell(sender As Object, e As EventArgs)
            If _settingsGridView.CurrentCell IsNot Nothing AndAlso Not _settingsGridView.IsCurrentCellInEditMode Then
                If EnsureCheckedOut() Then
                    _settingsGridView.BeginEdit(False)
                End If
            End If
        End Sub
        ''' <param name="Sender"></param>
        ''' <param name="e"></param>
        Private Sub MenuRemove(Sender As Object, e As EventArgs)
            ' Gotta check out files before removing anything...
            If Not EnsureCheckedOut() Then
                Return
            End If

            Dim rowsToDelete As New Dictionary(Of DataGridViewRow, Boolean)

            ' Find all rows with containing a selected cell
            For Each cell As DataGridViewCell In _settingsGridView.SelectedCells
                rowsToDelete(_settingsGridView.Rows(cell.RowIndex)) = True
            Next

            RemoveRows(rowsToDelete.Keys)
        End Sub

#End Region

#Region "IVsWindowPaneCommit implementation"
        Public Function IVsWindowPaneCommit_CommitPendingEdit(ByRef pfCommitFailed As Integer) As Integer Implements IVsWindowPaneCommit.CommitPendingEdit
            If CommitPendingChanges(False, False) Then
                pfCommitFailed = 0
            Else
                pfCommitFailed = 1
            End If
            Return NativeMethods.S_OK
        End Function
#End Region

        Private Sub ReportError(Message As String, HelpLink As String)
            ' Work around for VsWhidbey 224085 (app designer stealing the focus)
            Dim hwndFocus As IntPtr = NativeMethods.GetFocus()
            ' We also need to indicate that we are showing a modal dialog box so we don't try and commit 
            ' any pending changes 'cause of the change of active window...
            Dim savedReportingError As Boolean = _isReportingError
            Try
                _isReportingError = True
                DesignUtil.ReportError(Settings.Site, Message, HelpLink)
            Finally
                _isReportingError = savedReportingError
                ' Work around for VsWhidbey 224085 (app designer stealing my focus)
                If hwndFocus <> IntPtr.Zero Then
                    Switches.TracePDFocus(TraceLevel.Warning, "[disabled] SettingsDesignerView.ReportError focus hack: NativeMethods.SetFocus(hwndFocus)")
                    'NativeMethods.SetFocus(hwndFocus) - disabled this hack, it causes problems now that project designer is handling focus better
                End If
            End Try
        End Sub

        ''' <summary>
        ''' When the "Browse" item in the types combobox is selected, we want to pop a 
        ''' the type picker dialog...
        ''' </summary>
        Private Sub TypeComboBoxSelectedIndexChanged()
            Dim ptCurrent As Drawing.Point = _settingsGridView.CurrentCellAddress

            If ptCurrent.X <> TypeColumnNo Then
                Debug.Fail("We shouldn't browse for a type when the current cell isn't the type cell!")
                Return
            End If
            If _isShowingTypePicker Then
                Return
            End If

            Try
                _isShowingTypePicker = True
                If Not DBNull.Value.Equals(_settingsGridView.CurrentCell.Value) Then
                    Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                        Dim TypePickerDlg As New TypePickerDialog(Settings.Site, DesignerLoader.VsHierarchy, DesignerLoader.ProjectItemid)

                        TypePickerDlg.SetProjectReferencedAssemblies()

                        If UIService.ShowDialog(TypePickerDlg) = DialogResult.OK Then
                            ChangeSettingType(_settingsGridView.Rows(ptCurrent.Y), TypePickerDlg.TypeName)
                        Else
                            ' The user clicked cancel in the dialog - let's cancel this edit
                            _settingsGridView.CancelEdit()
                        End If
                    End Using
                End If
            Finally
                _isShowingTypePicker = False
            End Try
        End Sub

        ''' <summary>
        ''' If we want to sort the value column, we should sort the formatted values, not the
        ''' values themselves. 
        ''' If we want to sort the scope column, we should also sort the formatted value in order
        ''' to group web applications together...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' We are digging up the editedformattedvalue from the cell rather than serialize
        ''' the value that we get passed in every for perf. reasons...
        ''' </remarks>
        Private Sub OnSettingsGridViewSortCompare(sender As Object, e As DataGridViewSortCompareEventArgs) Handles _settingsGridView.SortCompare
            If e.Column.Index = ValueColumnNo OrElse e.Column.Index = ScopeColumnNo Then
                Dim strVal1 As String = TryCast(_settingsGridView.Rows(e.RowIndex1).Cells(e.Column.Index).EditedFormattedValue, String)
                Dim strVal2 As String = TryCast(_settingsGridView.Rows(e.RowIndex2).Cells(e.Column.Index).EditedFormattedValue, String)
                If strVal1 Is Nothing Then strVal1 = ""
                If strVal2 Is Nothing Then strVal2 = ""
                e.SortResult = StringComparer.CurrentCulture().Compare(strVal1, strVal2)
                e.Handled = True
            End If
        End Sub

        ''' <summary>
        ''' Receives broadcast messages passed on by the VS shell
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wParam"></param>
        ''' <param name="lParam"></param>
        Private Function OnBroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr) As Integer Implements IVsBroadcastMessageEvents.OnBroadcastMessage
            If msg = Interop.Win32Constant.WM_SETTINGCHANGE Then
                SetFonts()
            End If
        End Function

        ''' <summary>
        ''' Helper method to get a service from either our settings object or from out root designer
        ''' </summary>
        ''' <param name="service"></param>
        Protected Overrides Function GetService(service As Type) As Object
            Dim svc As Object = Nothing
            If Settings IsNot Nothing AndAlso Settings.Site IsNot Nothing Then
                svc = Settings.Site.GetService(service)
            End If

            If svc Is Nothing AndAlso _rootDesigner IsNot Nothing Then
                svc = _rootDesigner.GetService(service)
            End If

            If svc Is Nothing Then
                Return MyBase.GetService(service)
            Else
                Return svc
            End If
        End Function

        ''' <summary>
        ''' Calculate an appropriate row height for added rows...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnRowAdded(sender As Object, e As DataGridViewRowsAddedEventArgs) Handles _settingsGridView.RowsAdded
            Dim newRow As DataGridViewRow = _settingsGridView.Rows(e.RowIndex)
            newRow.Height = newRow.GetPreferredHeight(e.RowIndex, DataGridViewAutoSizeRowMode.AllCells, True)
        End Sub

        ''' <summary>
        ''' Whenever the font changes, we have to resize the row headers...
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnFontChanged(e As EventArgs)
            MyBase.OnFontChanged(e)
            _settingsGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells)
        End Sub

        Protected Overrides Sub OnLayout(levent As LayoutEventArgs)
            Switches.TracePDPerf("OnLayout BEGIN: SettingsDesignerView.OnLayout()")
            MyBase.OnLayout(levent)
            Switches.TracePDPerf("   OnLayout END: SettingsDesignerView.OnLayout()")
        End Sub

        Private ReadOnly Property DesignerLoader As SettingsDesignerLoader
            Get
                If _designerLoader Is Nothing Then
                    _designerLoader = TryCast(GetService(GetType(IDesignerLoaderService)), SettingsDesignerLoader)
                End If
                Return _designerLoader
            End Get
        End Property

    End Class

End Namespace
