' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Show a dialog allowing the user to pick a type
    ''' </summary>
    Friend NotInheritable Class TypePickerDialog
        'Inherits System.Windows.Forms.Form
        Inherits BaseDialog

        Private Shared s_previousSize As Size = Size.Empty

        Private ReadOnly _projectItemid As UInteger
        Private ReadOnly _vsHierarchy As IVsHierarchy

        Public Sub New(ServiceProvider As IServiceProvider, vsHierarchy As IVsHierarchy, ItemId As UInteger)
            MyBase.New(ServiceProvider)

            _vsHierarchy = vsHierarchy
            _projectItemid = ItemId

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            If Not s_previousSize.IsEmpty Then
                Size = New Size(Math.Min(CInt(MinimumSize.Width * 1.5), s_previousSize.Width),
                                                  Math.Min(CInt(MinimumSize.Height * 1.5), s_previousSize.Height))

            End If

            'Add any initialization after the InitializeComponent() call
            _typeTreeView = New TypeTV With {
                .AccessibleName = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_SelectATypeTreeView_AccessibleName,
                .Dock = DockStyle.Fill
            }
            AddHandler _typeTreeView.AfterSelect, AddressOf TypeTreeViewAfterSelectHandler
            AddHandler _typeTreeView.BeforeExpand, AddressOf TypeTreeViewBeforeExpandHandler
            _treeViewPanel.Controls.Add(_typeTreeView)
            F1Keyword = HelpIDs.Dlg_PickType
        End Sub

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
        Private WithEvents _typeTextBox As TextBox
        Private WithEvents _cancelButton As Button
        Private WithEvents _okButton As Button
        Private WithEvents _treeViewPanel As Panel
        Private WithEvents _selectedTypeLabel As Label
        Private WithEvents _okCancelTableLayoutPanel As TableLayoutPanel
        Private WithEvents _overarchingTableLayoutPanel As TableLayoutPanel

        'Required by the Windows Form Designer
        Private ReadOnly _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <DebuggerNonUserCode()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(TypePickerDialog))
            _typeTextBox = New TextBox
            _cancelButton = New Button
            _okButton = New Button
            _treeViewPanel = New Panel
            _selectedTypeLabel = New Label
            _okCancelTableLayoutPanel = New TableLayoutPanel
            _overarchingTableLayoutPanel = New TableLayoutPanel
            _okCancelTableLayoutPanel.SuspendLayout()
            _overarchingTableLayoutPanel.SuspendLayout()
            SuspendLayout()
            '
            'TypeTextBox
            '
            resources.ApplyResources(_typeTextBox, "TypeTextBox")
            _typeTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend
            _typeTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource
            _typeTextBox.Margin = New Padding(3, 3, 0, 3)
            _typeTextBox.Name = "TypeTextBox"
            '
            'm_CancelButton
            '
            resources.ApplyResources(_cancelButton, "m_CancelButton")
            _cancelButton.DialogResult = DialogResult.Cancel
            _cancelButton.Margin = New Padding(3, 0, 0, 0)
            _cancelButton.Name = "m_CancelButton"
            '
            'm_OkButton
            '
            resources.ApplyResources(_okButton, "m_OkButton")
            _okButton.Margin = New Padding(0, 0, 3, 0)
            _okButton.Name = "m_OkButton"
            '
            'TreeViewPanel
            '
            resources.ApplyResources(_treeViewPanel, "TreeViewPanel")
            _overarchingTableLayoutPanel.SetColumnSpan(_treeViewPanel, 2)
            _treeViewPanel.Margin = New Padding(0, 0, 0, 3)
            _treeViewPanel.Name = "TreeViewPanel"
            '
            'SelectedTypeLabel
            '
            resources.ApplyResources(_selectedTypeLabel, "SelectedTypeLabel")
            _selectedTypeLabel.Margin = New Padding(0, 3, 3, 3)
            _selectedTypeLabel.Name = "SelectedTypeLabel"
            '
            'okCancelTableLayoutPanel
            '
            resources.ApplyResources(_okCancelTableLayoutPanel, "okCancelTableLayoutPanel")
            _overarchingTableLayoutPanel.SetColumnSpan(_okCancelTableLayoutPanel, 2)
            _okCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
            _okCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
            _okCancelTableLayoutPanel.Controls.Add(_okButton, 0, 0)
            _okCancelTableLayoutPanel.Controls.Add(_cancelButton, 1, 0)
            _okCancelTableLayoutPanel.Margin = New Padding(0, 3, 0, 0)
            _okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"
            _okCancelTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(_overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            _overarchingTableLayoutPanel.ColumnStyles.Add(New ColumnStyle)
            _overarchingTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            _overarchingTableLayoutPanel.Controls.Add(_treeViewPanel, 0, 0)
            _overarchingTableLayoutPanel.Controls.Add(_okCancelTableLayoutPanel, 0, 2)
            _overarchingTableLayoutPanel.Controls.Add(_selectedTypeLabel, 0, 1)
            _overarchingTableLayoutPanel.Controls.Add(_typeTextBox, 1, 1)
            _overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            _overarchingTableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))
            _overarchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            _overarchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'TypePickerDialog
            '
            AcceptButton = _okButton
            resources.ApplyResources(Me, "$this")
            AutoScaleMode = AutoScaleMode.Font
            CancelButton = _cancelButton
            Controls.Add(_overarchingTableLayoutPanel)
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "TypePickerDialog"
            AutoScaleMode = AutoScaleMode.Font
            ShowIcon = False
            _okCancelTableLayoutPanel.ResumeLayout(False)
            _okCancelTableLayoutPanel.PerformLayout()
            _overarchingTableLayoutPanel.ResumeLayout(False)
            _overarchingTableLayoutPanel.PerformLayout()
            ResumeLayout(False)

        End Sub

#End Region

        Private ReadOnly _typeTreeView As TypeTV

        Private Sub TypeTreeViewAfterSelectHandler(sender As Object, e As TreeViewEventArgs)
            If e.Node IsNot Nothing Then
                Dim Node As TypeTVNode = TryCast(e.Node, TypeTVNode)
                If Node IsNot Nothing Then
                    If Node.IsTypeNode Then
                        _typeTextBox.Text = Node.Parent.Text + "." + Node.Text
                    End If
                End If
            End If
        End Sub

        Private Sub TypeTreeViewBeforeExpandHandler(sender As Object, e As TreeViewCancelEventArgs)
            If e.Node IsNot Nothing Then
                Dim Node As TypeTVNode = TryCast(e.Node, TypeTVNode)
                If Node IsNot Nothing AndAlso Node.IsAssemblyNode AndAlso Node.HasDummyNode Then
                    Node.RemoveDummyNode()
                    Using mtsrv As New VSDesigner.MultiTargetService(_vsHierarchy, _projectItemid, False)
                        If mtsrv IsNot Nothing Then
                            Dim availableTypes As Type() = mtsrv.GetSupportedTypes(Node.Text, AddressOf GetAssemblyCallback)
                            For Each availableType As Type In availableTypes

                                If availableType.FullName.Contains(".") Then

                                    ' NOTE that GetRuntimeType returns null when there's a failure resolving the type
                                    Dim runtimeType = mtsrv.GetRuntimeType(availableType)
                                    If runtimeType IsNot Nothing AndAlso SettingTypeValidator.IsValidSettingType(runtimeType) Then
                                        _typeTreeView.AddTypeNode(Node, availableType.FullName)
                                    End If

                                End If
                            Next
                        End If
                    End Using
                End If
            End If
        End Sub

        Private Function GetAssemblyCallback(an As AssemblyName) As Assembly
            Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
            If Resolver IsNot Nothing Then
                'Return Resolver.GetAssembly(an, False)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Get whatever type name the user selected
        ''' </summary>
        Public Property TypeName As String
            Get
                Return _typeTextBox.Text.Trim()
            End Get
            Set
                _typeTextBox.Text = Value
            End Set
        End Property

        Public Sub SetServiceProvider(Provider As IServiceProvider)
            ServiceProvider = Provider
        End Sub

        ''' <summary>
        ''' A collection of available types
        ''' </summary>
        Private ReadOnly Property AvailableTypes As AutoCompleteStringCollection
            Get
                Return _typeTextBox.AutoCompleteCustomSource
            End Get
        End Property

        Private Sub SetAvailableTypes(types As IEnumerable(Of Type))
            _typeTextBox.AutoCompleteCustomSource.Clear()
            If types IsNot Nothing Then
                For Each availableType As Type In types
                    _typeTextBox.AutoCompleteCustomSource.Add(availableType.FullName)
                    If availableType.FullName.Contains(".") Then
                        _typeTreeView.AddTypeNode(Nothing, availableType.FullName)
                    End If
                Next
            End If
        End Sub

        Public Sub SetProjectReferencedAssemblies()
            _typeTextBox.AutoCompleteCustomSource.Clear()

            Dim envDTE As EnvDTE.Project = DTEUtils.EnvDTEProject(_vsHierarchy)
            Dim VSProject As VSLangProj.VSProject = DirectCast(envDTE.Object, VSLangProj.VSProject)
            Dim References As VSLangProj.References = Nothing

            If VSProject IsNot Nothing Then
                References = VSProject.References()
            End If

            If References IsNot Nothing Then
                Using mtsrv As New VSDesigner.MultiTargetService(_vsHierarchy, _projectItemid, False)
                    For ReferenceNo As Integer = 1 To References.Count()
                        Dim reference As String = References.Item(ReferenceNo).Name()
                        If mtsrv Is Nothing OrElse mtsrv.IsSupportedAssembly(reference) Then
                            _typeTreeView.AddAssemblyNode(reference)
                        End If
                    Next
                End Using
            Else
                Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
                If Resolver IsNot Nothing Then
                    SetAvailableTypes(Resolver.GetWellKnownTypes())
                End If
            End If
        End Sub

        ''' <summary>
        ''' Try to validate the current type name, giving the user a chance to cancel the close
        ''' if validation fails
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OnOkButtonClick(sender As Object, e As EventArgs) Handles _okButton.Click
            If QueryClose() Then
                DialogResult = DialogResult.OK
                Hide()
            End If
        End Sub

        ''' <summary>
        ''' Validate the current type name, prompting the user if the validation fails
        ''' </summary>
        ''' <returns>
        ''' True if validation successful OR validation unsuccessful, but user chooses to save type anyway
        '''</returns>
        Private Function QueryClose() As Boolean
            Dim ShouldClose As Boolean

            Dim NormalizedTypeName As String
            Try
                NormalizedTypeName = NormalizeTypeName(TypeName)

                Dim Resolver As SettingsTypeCache = DirectCast(GetService(GetType(SettingsTypeCache)), SettingsTypeCache)
                Debug.Assert(Resolver IsNot Nothing, "Couldn't find a SettingsTypeCache")
                Dim resolvedType As Type = Resolver.GetSettingType(NormalizedTypeName)
                If resolvedType Is Nothing Then
                    ' This shouldn't normally happen - if we were able to figure out what the
                    ' display name is, we should be able to figure out what the type is...
                    ' We failed to resolve the type...
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UnknownType, TypeName), TypeName)
                    ShouldClose = False
                ElseIf resolvedType.IsGenericType Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_GenericTypesNotSupported_1Arg, TypeName))
                    ShouldClose = False
                ElseIf resolvedType.IsAbstract Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_AbstractTypesNotSupported_1Arg, TypeName))
                    ShouldClose = False
                ElseIf Not SettingTypeValidator.IsValidSettingType(resolvedType) Then
                    ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UnknownType, TypeName), TypeName)
                    ShouldClose = False
                Else
                    ' Everything is cool'n froody!
                    TypeName = NormalizedTypeName
                    ShouldClose = True
                End If

            Catch ex As ArgumentException
                ' The type resolution may throw an argument exception if the type name was invalid...
                ' Let's report the error and keep the dialog open!
                ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            Catch ex As FileLoadException
                ' The type resolution may throw an argument exception if the type name contains an invalid assembly name 
                ' (i.e. Foo,,)
                ' Let's report the error and keep the dialog open!
                ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            Catch ex As Exception When ReportWithoutCrash(ex, $"Unexpected exception caught when resolving type {TypeName}", NameOf(TypePickerDialog))
                ' We don't know what happened here - let's assume that the type name was bad...
                ' Let's report the error and keep the dialog open!
                ReportError(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidTypeName_1Arg, TypeName))
                Return False
            End Try
            Return ShouldClose
        End Function

        ''' <summary>
        ''' Get the correct type name from what the user typed in the text box. The textbox accepts language specific
        ''' type names (i.e. int for System.Int32) as well as type names in imported namespaces
        ''' </summary>
        ''' <param name="displayName"></param>
        Private Function NormalizeTypeName(displayName As String) As String
            Dim typeNameResolutionService As SettingTypeNameResolutionService =
                DirectCast(GetService(GetType(SettingTypeNameResolutionService)), SettingTypeNameResolutionService)

            Debug.Assert(typeNameResolutionService IsNot Nothing, "The settingsdesignerloader should have added a typenameresolutioncomponent service!")
            If typeNameResolutionService IsNot Nothing Then
                displayName = typeNameResolutionService.TypeDisplayNameToPersistedSettingTypeName(displayName)
            End If

            Dim typeNameCache As Dictionary(Of String, Object) = Nothing
            If Not typeNameResolutionService.IsCaseSensitive Then
                typeNameCache = New Dictionary(Of String, Object)(StringComparison.OrdinalIgnoreCase)
                For Each typeName As String In AvailableTypes
                    typeNameCache(typeName) = Nothing
                Next
            End If

            If typeNameCache IsNot Nothing Then
                If typeNameCache.ContainsKey(displayName) Then
                    Return displayName
                End If
            Else
                If AvailableTypes.Contains(displayName) Then
                    Return displayName
                End If
            End If

            For Each import As String In GetProjectImports()
                Dim probeName As String = String.Format("{0}.{1}", import, displayName)
                If typeNameCache IsNot Nothing Then
                    If typeNameCache.ContainsKey(probeName) Then
                        Return probeName
                    End If
                Else
                    If AvailableTypes.Contains(probeName) Then
                        Return probeName
                    End If
                End If
            Next
            Return displayName
        End Function

        ''' <summary>
        ''' Get the project level imports
        ''' </summary>
        Private Function GetProjectImports() As List(Of String)
            Dim Result As New List(Of String)
            Dim vsProject As VSLangProj.VSProject
            vsProject = TryCast(GetService(GetType(VSLangProj.VSProject)), VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso vsProject.Imports() IsNot Nothing Then
                For Index As Integer = 1 To vsProject.Imports.Count
                    Result.Add(vsProject.Imports.Item(Index))
                Next
            Else
                ' CONSIDER: add some default "imports" here (like "System")
            End If
            Return Result
        End Function

        Private Class TypeTV
            Inherits TreeView

            Public Sub New()
                PathSeparator = "."
                Sorted = True

                'Scale the imagelist for High DPI
                Dim newSize = DpiAwareness.LogicalToDeviceSize(Handle, New Size(96, 96))

                Dim assemblyImage As Image = GetImageFromImageService(KnownMonikers.Assembly, newSize.Width, newSize.Height, Color.Transparent)
                Dim namespaceImage As Image = GetImageFromImageService(KnownMonikers.Namespace, newSize.Width, newSize.Height, Color.Transparent)
                Dim objectImage As Image = GetImageFromImageService(KnownMonikers.ClassPublic, newSize.Width, newSize.Height, Color.Transparent)

                Dim treeViewIcons As ImageList = New ImageList()
                treeViewIcons.Images.Add(assemblyImage)
                treeViewIcons.Images.Add(namespaceImage)
                treeViewIcons.Images.Add(objectImage)

                ImageList = treeViewIcons

                DTEUtils.ApplyTreeViewThemeStyles(Handle)
            End Sub

            Public Sub AddAssemblyNode(assemblyName As String)
                If Not String.IsNullOrEmpty(assemblyName) AndAlso Not Nodes.ContainsKey(assemblyName) Then
                    Dim asNode As TypeTVNode = New TypeTVNode(NodeType.ASSEMBLY_NODE) With {
                        .Text = assemblyName,
                        .Name = assemblyName
                    }
                    Nodes.Add(asNode)
                    asNode.AddDummyNode()
                End If
            End Sub

            Public Sub AddTypeNode(asNode As TypeTVNode, typeFullName As String)
                Dim nodes As TreeNodeCollection = Me.Nodes
                If asNode IsNot Nothing Then
                    nodes = asNode.Nodes
                End If
                Dim nsName As String = TypeTVNode.ExtractName(typeFullName)
                Dim typName As String = TypeTVNode.ExtractChildPath(typeFullName)
                If Not String.IsNullOrEmpty(nsName) Then
                    Dim nsNode As TypeTVNode

                    If Not nodes.ContainsKey(nsName) Then
                        nsNode = New TypeTVNode(NodeType.NAMESPACE_NODE) With {
                            .Text = nsName,
                            .Name = nsName
                        }
                        nodes.Add(nsNode)
                    Else
                        nsNode = DirectCast(nodes(nsName), TypeTVNode)
                    End If
                    If Not String.IsNullOrEmpty(typName) AndAlso Not nsNode.Nodes.ContainsKey(typName) Then
                        Dim typNode As TypeTVNode = New TypeTVNode(NodeType.TYPE_NODE) With {
                            .Text = typName,
                            .Name = typName
                        }
                        nsNode.Nodes.Add(typNode)
                    End If
                End If
            End Sub

        End Class

        Private Enum NodeType
            ASSEMBLY_NODE
            NAMESPACE_NODE
            TYPE_NODE
        End Enum

        Private Class TypeTVNode
            Inherits TreeNode

            Private ReadOnly _nodeType As NodeType

            Private Const DUMMY_ITEM_TEXT As String = " THIS IS THE DUMMY ITEM "
            Private Const AssemblyImageIndex As Integer = 0
            Private Const SelectedAssemblyImageIndex As Integer = 0
            Private Const NamespaceImageIndex As Integer = 1
            Private Const SelectedNamespaceImageIndex As Integer = 1
            Private Const TypeImageIndex As Integer = 2
            Private Const SelectedTypeImageIndex As Integer = 2

            Public Sub New(nodeType As NodeType)
                _nodeType = nodeType

                Select Case nodeType
                    Case NodeType.ASSEMBLY_NODE
                        ImageIndex = AssemblyImageIndex
                        SelectedImageIndex = SelectedAssemblyImageIndex
                    Case NodeType.NAMESPACE_NODE
                        ImageIndex = NamespaceImageIndex
                        SelectedImageIndex = SelectedNamespaceImageIndex
                    Case NodeType.TYPE_NODE
                        ImageIndex = TypeImageIndex
                        SelectedImageIndex = SelectedTypeImageIndex
                End Select

            End Sub

            Public ReadOnly Property IsAssemblyNode As Boolean
                Get
                    Return _nodeType = NodeType.ASSEMBLY_NODE
                End Get
            End Property

            Public ReadOnly Property HasDummyNode As Boolean
                Get
                    Return Nodes.ContainsKey(DUMMY_ITEM_TEXT)
                End Get
            End Property

            Public ReadOnly Property IsNameSpaceNode As Boolean
                Get
                    Return _nodeType = NodeType.NAMESPACE_NODE
                End Get
            End Property

            Public ReadOnly Property IsTypeNode As Boolean
                Get
                    Return _nodeType = NodeType.TYPE_NODE
                End Get
            End Property

            Public Sub AddDummyNode()
                If IsAssemblyNode() AndAlso Nodes.Count = 0 Then
                    Nodes.Add(DUMMY_ITEM_TEXT, DUMMY_ITEM_TEXT)
                End If
            End Sub

            Public Sub RemoveDummyNode()
                If IsAssemblyNode() AndAlso Nodes.ContainsKey(DUMMY_ITEM_TEXT) Then
                    Nodes.RemoveByKey(DUMMY_ITEM_TEXT)
                End If
            End Sub

#If DRILL_DOWN_NAMESPACES Then
            Friend Shared Function ExtractName(Path As String) As String
                Dim PointPos As Integer = Path.IndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(0, PointPos)
                Else
                    Return Path
                End If
            End Function

            Friend Shared Function ExtractChildPath(Path As String) As String
                Dim PointPos As Integer = Path.IndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(PointPos + 1)
                Else
                    Return ""
                End If
            End Function
#Else
            Friend Shared Function ExtractName(Path As String) As String
                Dim PointPos As Integer = Path.LastIndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(0, PointPos)
                Else
                    Return Path
                End If
            End Function

            Friend Shared Function ExtractChildPath(Path As String) As String
                Dim PointPos As Integer = Path.LastIndexOf(".")
                If PointPos <> -1 Then
                    Return Path.Substring(PointPos + 1)
                Else
                    Return ""
                End If
            End Function
#End If
        End Class

        ''' <summary>
        ''' We want to preserve the size of the dialog for the next time the user selects it...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub TypePickerDialog_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
            s_previousSize = Size
        End Sub

        Private Sub TypePickerDialog_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class
End Namespace
