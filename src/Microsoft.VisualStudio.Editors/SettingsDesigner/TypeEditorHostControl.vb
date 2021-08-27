' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Design
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Control to host UI type editor. Set the value and value type of the value to edit
    ''' </summary>
    Friend Class TypeEditorHostControl
        Inherits UserControl
        Implements IWindowsFormsEditorService, IServiceProvider

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer. 
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call 
            _previewPanel.BackColor = ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_WINDOW, SystemColors.Window, UseVSTheme:=False)
            BackColor = ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_WINDOW, SystemColors.Window, UseVSTheme:=False)
            _editControls = New Control() {_valueTextBox, _valueComboBox}
            EditControl = _valueTextBox
        End Sub

        'UserControl1 overrides dispose to clean up the component list. 
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer 
        Private ReadOnly _components As IContainer

        'NOTE: The following procedure is required by the Windows Form Designer 
        'It can be modified using the Windows Form Designer. 
        'Do not modify it using the code editor. 
        Private WithEvents _valueTextBox As TextBox
        Private WithEvents _showEditorButton As ComboBoxDotDotDotButton
        Private WithEvents _valueComboBox As ComboBox
        Private WithEvents _previewPanel As Panel
        <DebuggerStepThrough()> Private Sub InitializeComponent()

            Dim resources As ComponentResourceManager = New ComponentResourceManager(GetType(TypeEditorHostControl))
            _valueTextBox = New TypeEditorHostControlTextBox
            _showEditorButton = New ComboBoxDotDotDotButton
            _previewPanel = New Panel
            _valueComboBox = New ComboBox
            SuspendLayout()
            '
            'ValueTextBox
            '
            resources.ApplyResources(_valueTextBox, "ValueTextBox")
            _valueTextBox.BorderStyle = BorderStyle.None
            _valueTextBox.Name = "ValueTextBox"
            '
            'ShowEditorButton
            '
            resources.ApplyResources(_showEditorButton, "ShowEditorButton")
            _showEditorButton.BackColor = ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_THREEDFACE,
                                                                        SystemColors.ButtonFace, UseVSTheme:=False)
            _showEditorButton.Name = "ShowEditorButton"
            _showEditorButton.UseVisualStyleBackColor = False
            '
            'PreviewPanel
            '
            resources.ApplyResources(_previewPanel, "PreviewPanel")
            _previewPanel.Name = "PreviewPanel"
            '
            'ValueComboBox
            '
            _valueComboBox.FormattingEnabled = True
            resources.ApplyResources(_valueComboBox, "ValueComboBox")
            _valueComboBox.Name = "ValueComboBox"
            '
            'TypeEditorHostControl
            '
            Controls.Add(_valueComboBox)
            Controls.Add(_previewPanel)
            Controls.Add(_showEditorButton)
            Controls.Add(_valueTextBox)
            Name = "TypeEditorHostControl"
            resources.ApplyResources(Me, "$this")
            ResumeLayout(False)
            PerformLayout()

        End Sub

#End Region

#Region "Private fields"
        Private _typeEditor As UITypeEditor
        Private _typeConverter As TypeConverter
        Private _innerValue As Object
        Private _innerValueType As Type

        ''' <summary>
        ''' Flag indicating that the text in the editing control is changed, and
        ''' that we need to re-parse it if the user wants to get the deserialized
        ''' value
        ''' </summary>
        Private _textValueDirty As Boolean

        ' Indicating if we are currently showing a UI type editor
        Private _isShowingUITypeEditor As Boolean

        Private _currentEditControl As Control
        Private ReadOnly _editControls() As Control

        ' Holder window for drop-downs...
        Private _dialog As DropDownHolder

        ' Flag to avoid fireing value change notifications when we programatically set the
        ' text...
        Private _ignoreTextChangeEvents As Boolean

#End Region

#Region "Value & value type properties for value to edit"

        Public Property ValueType As Type
            Get
                Return _innerValueType
            End Get
            Set
                _innerValueType = Value

                ' Let's try and get a UITypeEditor for this type! 
                _typeEditor = GetSpecificEditorForType(Value)
                If _typeEditor Is Nothing Then
                    If Value Is GetType(String) Then
                        ' We'll use the multiline string editor for strings...
                        _typeEditor = New Design.MultilineStringEditor()
                    Else
                        _typeEditor = CType(TypeDescriptor.GetEditor(Value, GetType(UITypeEditor)), UITypeEditor)
                    End If
                End If

                ' Cache a type converter 
                _typeConverter = TypeDescriptor.GetConverter(Value)

                ' If we have a type editor, let's see if it supports preview of 
                ' the value... 
                Dim PreviewSupported As Boolean = False
                If _typeEditor IsNot Nothing Then
                    PreviewSupported = _typeEditor.GetPaintValueSupported()
                End If
                _previewPanel.Visible = PreviewSupported

                ' We should show the "button" if (and only if) we have a valid 
                ' UITypeEditor!
                Dim ShowEditorButtonVisible As Boolean = False
                If _typeEditor IsNot Nothing Then
                    Select Case _typeEditor.GetEditStyle()
                        Case UITypeEditorEditStyle.DropDown
                            _showEditorButton.PaintStyle = ComboBoxDotDotDotButton.PaintStyles.DropDown
                            ShowEditorButtonVisible = True
                        Case UITypeEditorEditStyle.Modal
                            _showEditorButton.PaintStyle = ComboBoxDotDotDotButton.PaintStyles.DotDotDot
                            ShowEditorButtonVisible = True
                    End Select
                End If
                _showEditorButton.Visible = ShowEditorButtonVisible

                ' If we are showing something that supports GetStandardValues, but doesn't have a UITypeEditor, we'll 
                ' show a nice combobox instead of that boring edit box!
                If (Not ShowEditorButtonVisible) _
                    AndAlso _typeConverter IsNot Nothing _
                    AndAlso _typeConverter.GetStandardValuesSupported() _
                    AndAlso _typeConverter.GetStandardValues().Count() > 0 _
                Then
                    EditControl = _valueComboBox
                    _valueComboBox.Items.Clear()
                    For Each stdValue As Object In _typeConverter.GetStandardValues()
                        _valueComboBox.Items.Add(stdValue)
                    Next
                Else
                    EditControl = _valueTextBox
                End If

                ' If the preview panel is visible, we've gotta make sure we draw the
                ' new value on it! 
                If _previewPanel.Visible Then
                    _previewPanel.Invalidate()
                End If
            End Set
        End Property

        Public Property Value As Object
            Get
                If TextValueDirty Then
                    _innerValue = ParseValue(EditControl.Text, ValueType)
                    TextValueDirty = False
                End If
                Return _innerValue
            End Get
            Set
                If Value IsNot Nothing Then
                    ValueType = Value.GetType()
                End If
                _innerValue = Value
                Text = FormatValue(Value)
                ' If the preview panel is visible, we've gotta make sure we draw the
                ' new value on it! 
                If _previewPanel.Visible Then
                    _previewPanel.Invalidate()
                End If
            End Set
        End Property

        Protected ReadOnly Property InnerValue As Object
            Get
                Return _innerValue
            End Get
        End Property

        Protected Overridable Function GetSpecificEditorForType(KnownType As Type) As UITypeEditor
            Return Nothing
        End Function

        Protected Overridable Function FormatValue(ValueToFormat As Object) As String
            Return _typeConverter.ConvertToString(ValueToFormat)
        End Function

        Protected Overridable Function ParseValue(SerializedValue As String, ValueType As Type) As Object
            Return _typeConverter.ConvertFromString(SerializedValue)
        End Function

#End Region

#Region "Paint & layout of control"
        ''' <summary>
        ''' Paint the preview panel
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub PreviewPanel_Paint(sender As Object, e As PaintEventArgs) Handles _previewPanel.Paint
            If _typeEditor IsNot Nothing Then
                If _typeEditor.GetPaintValueSupported Then
                    Using ForegroundPen As New Pen(ForeColor)
                        Dim DrawRect As New Rectangle(1, 1, _previewPanel.ClientRectangle.Width - 4, _previewPanel.ClientRectangle.Height - 4)
                        If Value IsNot Nothing Then
                            _typeEditor.PaintValue(Value, e.Graphics, DrawRect)
                        End If
                        e.Graphics.DrawRectangle(ForegroundPen, DrawRect)
                    End Using
                End If
            End If
        End Sub

        ''' <summary>
        ''' Layout contained controls
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnLayout(e As LayoutEventArgs)
            MyBase.OnLayout(e)

            ' Left position of text box - will be bumped by preview panel if showing
            Dim TextBoxLeft As Integer = 0

            ' Width of text box - will be changed if preview panel and/or browse button
            ' is showing. Initially assume the text box is the only control showing
            Dim TextBoxWidth As Integer = Width

            ' All controls have the same height!
            _previewPanel.Height = Height
            EditControl.Height = Height

            _showEditorButton.Height = SystemInformation.VerticalScrollBarThumbHeight + 2 * _showEditorButton.FlatAppearance.BorderSize
            _showEditorButton.Width = SystemInformation.VerticalScrollBarWidth + 2 * _showEditorButton.FlatAppearance.BorderSize

            ' Let's make the preview panel nice and square...
            _previewPanel.Width = _previewPanel.Height

            ' If the preview panel is showing, bump the text box right
            ' and decrease it's width
            If _previewPanel.Visible Then
                TextBoxWidth -= _previewPanel.Width
                TextBoxLeft += _previewPanel.Width
            End If

            ' If we show the browse button, decrease the text box width
            If _showEditorButton.Visible Then
                TextBoxWidth -= _showEditorButton.Width
            End If

            ' Position controls
            EditControl.Left = TextBoxLeft
            EditControl.Width = TextBoxWidth

            _showEditorButton.Top = 0
            _showEditorButton.Left = EditControl.Left + EditControl.Width
        End Sub

#End Region

#Region "Misc. control event handlers"
        ''' <summary>
        ''' Use the UI Type editor to edit the current value
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ShowEditorButton_Click(sender As Object, e As EventArgs) Handles _showEditorButton.Click
            Debug.Assert(_typeEditor IsNot Nothing)
            ShowUITypeEditor()
        End Sub

        ''' <summary>
        ''' Display the associated type editor if not already showing
        ''' </summary>
        <Security.SecurityCritical>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions>
        Private Sub ShowUITypeEditor()
            If _typeEditor IsNot Nothing Then
                If _isShowingUITypeEditor Then
                    Return
                End If

                _isShowingUITypeEditor = True
                Try
                    ' If this is a type that implements IList, we try to create a new instance before 
                    ' passing it to the UITypeEditor. Not doing so will show the UITypeEditor, everything will
                    ' look fine, but when closing the UITypeEditor, it will still return nothing :(
                    Dim passedNewInstanceToEditor As Boolean = False
                    Dim existingValue As Object = Value
                    If Value Is Nothing AndAlso GetType(IList).IsAssignableFrom(ValueType) Then
                        Try
                            existingValue = Activator.CreateInstance(ValueType)
                            passedNewInstanceToEditor = True
                        Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ShowUITypeEditor), NameOf(TypeEditorHostControl))
                        End Try
                    End If
                    Dim editedValue As Object = _typeEditor.EditValue(Context, Me, existingValue)

                    ' If we created a new instance to pass to the UITypeEditor, and the user didn't add any 
                    ' items, then we set the value back to nothing...
                    If passedNewInstanceToEditor Then
                        Dim valueAsIList As IList = TryCast(editedValue, IList)
                        If valueAsIList IsNot Nothing AndAlso valueAsIList.Count = 0 Then
                            editedValue = Nothing
                        End If
                    End If
                    Value = editedValue
                    OnValueChanged()
                    Switches.TracePDFocus(TraceLevel.Warning, "SettingsDesignerView.TypeEditorHostControl.ShowUITypeEditor.Me.Focus()")
                    Focus()
                Catch Ex As Exception When TypeOf Ex IsNot System.Threading.ThreadAbortException _
                    AndAlso TypeOf Ex IsNot AccessViolationException _
                    AndAlso TypeOf Ex IsNot StackOverflowException

                    Dim sp As IServiceProvider = VBPackage.Instance
                    DesignerFramework.DesignerMessageBox.Show(
                                       sp,
                                       "",
                                       Ex,
                                       DesignerFramework.DesignUtil.GetDefaultCaption(sp))
                Finally
                    _isShowingUITypeEditor = False
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Whenever the value in the textbox changes we may want to reset our value
        ''' PERF: will this cause perf problems?
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub TextChangedHandler(sender As Object, e As EventArgs) Handles _valueTextBox.TextChanged, _valueComboBox.TextChanged
            If Not _ignoreTextChangeEvents Then
                TextValueDirty = True
                OnValueChanged()
            End If
        End Sub

        ''' <summary>
        ''' Alt-Down should show the ui type editor if we have a drop-down type editor associated with 
        ''' the current type...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub KeyDownHandler(sender As Object, e As KeyEventArgs) Handles _valueTextBox.KeyDown
            If _typeEditor IsNot Nothing Then
                If _typeEditor.GetEditStyle() = UITypeEditorEditStyle.DropDown Then
                    If e.Alt AndAlso (e.KeyCode And Keys.KeyCode) = Keys.Down Then
                        If Not _isShowingUITypeEditor Then
                            ShowUITypeEditor()
                            e.Handled = True
                            Return
                        End If
                    End If
                ElseIf _typeEditor.GetEditStyle() = UITypeEditorEditStyle.Modal Then
                    If (e.KeyCode And Keys.KeyCode) = Keys.Enter Then
                        If Not _isShowingUITypeEditor Then
                            ShowUITypeEditor()
                            e.Handled = True
                            Return
                        End If
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Grab the enter key if we have the focus on the uitypeeditor button
        ''' and the current UI Type editor is a modal editor...
        ''' </summary>
        ''' <param name="keyData"></param>
        Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
            If keyData = Keys.Enter Then
                If _showEditorButton.Focused AndAlso _typeEditor IsNot Nothing AndAlso _typeEditor.GetEditStyle() = UITypeEditorEditStyle.Modal Then
                    Return True
                End If
            End If
            Return MyBase.IsInputKey(keyData)
        End Function

#End Region

#Region "IWindowsFormsEditorService"

        Public Sub CloseDropDown() Implements IWindowsFormsEditorService.CloseDropDown
            Debug.Assert(_dialog.Controls.Count = 1)
            _dialog.Hide()
        End Sub

        Private Sub DropDownHolderSizeChanged(sender As Object, e As EventArgs)
            DropDownHolderSize(TryCast(sender, Control))
        End Sub

        Private Sub DropDownHolderSize(control As Control)
            If _dialog IsNot Nothing AndAlso control IsNot Nothing Then

                ' Calculate size & position
                Dim currentScreen As Screen = Screen.FromControl(Me)

                ' Get preferred size & position of control...
                Dim dialogSize As Size = New Size(control.PreferredSize.Width + 2, control.PreferredSize.Height + 2)
                Dim UpperLeft As Point = PointToScreen(New Point(Width - dialogSize.Width, _showEditorButton.Height))

                ' If the dialog gets clipped at the bottom of the screen, let's try to reposition it above the
                ' edit control...
                If UpperLeft.Y + dialogSize.Height > currentScreen.WorkingArea.Bottom Then
                    UpperLeft.Y = UpperLeft.Y - Height - dialogSize.Height
                End If

                ' If the dialog gets clipped at the right of the screen, let's try to move it left...
                If UpperLeft.X + dialogSize.Width > currentScreen.WorkingArea.Right Then
                    UpperLeft.X = currentScreen.WorkingArea.Right - dialogSize.Width
                End If

                ' If, after all this moving, we are above/to the left of the screen, let's move
                ' it right/down again
                UpperLeft.X = Math.Max(UpperLeft.X, currentScreen.WorkingArea.Left)
                UpperLeft.Y = Math.Max(UpperLeft.Y, currentScreen.WorkingArea.Top)

                ' If the dialog wants to be larger than the screen, shrink it!
                dialogSize.Height = Math.Min(currentScreen.WorkingArea.Height, dialogSize.Height)
                dialogSize.Width = Math.Min(currentScreen.WorkingArea.Width, dialogSize.Width)

                _dialog.Size = dialogSize
                _dialog.Left = UpperLeft.X
                _dialog.Top = UpperLeft.Y
            End If
        End Sub

        Public Sub DropDownControl(control As Control) Implements IWindowsFormsEditorService.DropDownControl
            If _dialog Is Nothing Then
                _dialog = New DropDownHolder
            End If

            ' Let's make sure we don't have any child controls! 
            _dialog.Controls.Clear()
            DropDownHolderSize(control)
            AddHandler control.SizeChanged, AddressOf DropDownHolderSizeChanged
            _dialog.Editor = control
            _dialog.TopLevel = True
            _dialog.Owner = ParentForm
            _dialog.ShowInTaskbar = False
            _dialog.Show()
            _dialog.Activate()
            While _dialog.Visible
                Application.DoEvents()
                Interop.NativeMethods.MsgWaitForMultipleObjects(0, IntPtr.Zero, True, 250, Interop.Win32Constant.QS_ALLINPUT)
            End While
            RemoveHandler control.SizeChanged, AddressOf DropDownHolderSizeChanged
        End Sub

        Public Function ShowDialog(dialog As Form) As DialogResult Implements IWindowsFormsEditorService.ShowDialog
            Dim UiSvc As IUIService = DirectCast(GetService(GetType(IUIService)), IUIService)
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                If UiSvc IsNot Nothing Then
                    Return UiSvc.ShowDialog(dialog)
                Else
                    Return dialog.ShowDialog(_showEditorButton)
                End If
            End Using
        End Function

#End Region

        ''' <summary>
        ''' Are we currently showing the UI type editor?
        ''' </summary>
        Public ReadOnly Property IsShowingUITypeEditor As Boolean
            Get
                Return _isShowingUITypeEditor
            End Get
        End Property

        Protected Overridable Sub OnValueChanged()
        End Sub

#Region "IServiceProvider implementation"
        Public Function IServiceProvider_GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
            If serviceType.Equals(GetType(IWindowsFormsEditorService)) Then
                Return Me
            Else
                Return GetService(serviceType)
            End If
        End Function
#End Region

        ''' <summary>
        ''' Host the UI type editor control given to use. 
        ''' </summary>
        Private Class DropDownHolder
            Inherits Form

            Public Sub New()
                ' We don't want this form showing up in the task bar...
                ShowInTaskbar = False
            End Sub

            ''' <summary>
            ''' Override default create parameters for window
            ''' </summary>
            Protected Overrides ReadOnly Property CreateParams As CreateParams
                Get
                    Dim BaseParams As CreateParams = MyBase.CreateParams

                    Dim Params As New CreateParams With {
                        .ClassStyle = 0,
                        .Style = Constants.WS_VISIBLE Or Constants.WS_POPUP Or Constants.WS_BORDER,
                        .ExStyle = Constants.WS_EX_TOPMOST Or Constants.WS_EX_TOOLWINDOW,
                        .Height = Height,
                        .Width = Width,
                        .X = Left,
                        .Y = Top
                    }
                    Return Params
                End Get
            End Property

            ''' <summary>
            ''' Hide the form
            ''' </summary>
            ''' <remarks>Additional common "cleanup" code before closing the window goes here</remarks>
            Protected Sub HideForm()
                Hide()
            End Sub
            ''' <param name="e"></param>
            Protected Overrides Sub OnDeactivate(e As EventArgs)
                HideForm()
            End Sub

            ''' <summary>
            ''' Get/set the UI type editor that I'm hosting...
            ''' </summary>
            Public Property Editor As Control
                Get
                    If Controls.Count = 1 Then
                        Return Controls.Item(0)
                    Else
                        Return Nothing
                    End If
                End Get
                Set
                    Controls.Clear()
                    If Value IsNot Nothing Then
                        Value.Dock = DockStyle.Fill
                        Controls.Add(Value)
                    End If
                End Set
            End Property

            ''' <summary>
            ''' Pressing the escape key should close the window
            ''' </summary>
            ''' <param name="m"></param>
            Protected Overrides Function ProcessKeyPreview(ByRef m As Message) As Boolean
                If m.Msg = Interop.NativeMethods.WM_KEYDOWN Then
                    If CType(m.WParam.ToInt32(), Keys) = Keys.Escape Then
                        HideForm()
                        Return True
                    End If
                End If
                Return MyBase.ProcessKeyEventArgs(m)
            End Function
        End Class

        ''' <summary>
        ''' Whenever the selected index changes in the ValueComboBox, we update the value!
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ValueComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles _valueComboBox.SelectedIndexChanged
            _innerValue = _valueComboBox.SelectedItem
            TextValueDirty = False
            OnValueChanged()
        End Sub

        ''' <summary>
        ''' Is the text in the currently selected edit control dirty?
        ''' </summary>
        Private Property TextValueDirty As Boolean
            Get
                Return _textValueDirty
            End Get
            Set
                _textValueDirty = value
            End Set
        End Property

        ''' <summary>
        ''' Map the SelectAll procedure to the currently selected edit control
        ''' </summary>
        Public Sub SelectAll()
            If TypeOf EditControl Is ComboBox Then
                DirectCast(EditControl, ComboBox).SelectAll()
            Else
                Debug.Assert(TypeOf EditControl Is TextBox, "Unkown edit type!? " & EditControl.GetType().FullName)
                DirectCast(EditControl, TextBox).SelectAll()
            End If
        End Sub

        ''' <summary>
        ''' Map the selection length property to the currently selected edit control
        ''' </summary>
        Public Property SelectionLength As Integer
            Get
                If _valueComboBox.Visible Then
                    Return _valueComboBox.SelectionLength
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    Return _valueTextBox.SelectionLength
                End If
            End Get
            Set
                If _valueComboBox.Visible Then
                    _valueComboBox.SelectionLength = value
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    _valueTextBox.SelectionLength = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Map the selection start property to the currently selected edit control
        ''' </summary>
        Public Property SelectionStart As Integer
            Get
                If _valueComboBox.Visible Then
                    Return _valueComboBox.SelectionStart
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    Return _valueTextBox.SelectionStart
                End If
            End Get
            Set
                If _valueComboBox.Visible Then
                    _valueComboBox.SelectionStart = value
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    _valueTextBox.SelectionStart = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Map the selected text to the currently selected edit control
        ''' </summary>
        Public Property SelectedText As String
            Get
                If _valueComboBox.Visible Then
                    Return _valueComboBox.SelectedText
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    Return _valueTextBox.SelectedText
                End If
            End Get
            Set
                If _valueComboBox.Visible Then
                    _valueComboBox.SelectedText = value
                Else
                    Debug.Assert(_valueTextBox.Visible, "No edit control visible!?")
                    _valueTextBox.SelectedText = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Get the text in the edit control
        ''' </summary>
        Public Overrides Property Text As String
            Get
                Return EditControl.Text
            End Get
            Set
                Dim savedIgnoreTextChangeEvents As Boolean = _ignoreTextChangeEvents
                Try
                    _ignoreTextChangeEvents = True
                    EditControl.Text = value
                Finally
                    _ignoreTextChangeEvents = savedIgnoreTextChangeEvents
                End Try
            End Set
        End Property

        ''' <summary>
        ''' Get the currently active edit control (textbox or combobox)
        ''' </summary>
        Friend Property EditControl As Control
            Get
                If _currentEditControl IsNot Nothing Then
                    Return _currentEditControl
                Else
                    Return _valueTextBox
                End If
            End Get
            Set
                For Each ctrl As Control In _editControls
                    If ctrl Is value Then
                        ctrl.Visible = True
                    Else
                        ctrl.Visible = False
                    End If
                Next
                _currentEditControl = value
            End Set
        End Property

        ''' <summary>
        ''' Get an instance of a ITypeDescriptorContext to pass into the UITypeEditor
        ''' </summary>
        Public Overridable ReadOnly Property Context As ITypeDescriptorContext
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' We want to special-handle a couple of keyboard messages from the textbox...
        ''' </summary>
        Private Class TypeEditorHostControlTextBox
            Inherits TextBox

            ''' <summary>
            ''' This code was mainly ripped from DataGridViewTextBoxEditingControl...
            ''' </summary>
            ''' <param name="m"></param>
            <Security.Permissions.SecurityPermission(Security.Permissions.SecurityAction.LinkDemand, Flags:=Security.Permissions.SecurityPermissionFlag.UnmanagedCode)>
            Protected Overrides Function ProcessKeyEventArgs(ByRef m As Message) As Boolean
                Select Case CType(CInt(m.WParam), Keys)
                    Case Keys.Enter
                        ' REGISB: Check if WM_IME_CHAR needs to be treated the same.
                        If m.Msg = Interop.NativeMethods.WM_CHAR AndAlso Not ModifierKeys = Keys.Shift Then
                            ' Ignore the Enter key and don't add it to the textbox content. This happens when failing validation brings
                            ' up a dialog box for example.
                            ' Shift-Enter for multiline textboxes need to be accepted however.
                            Return True
                        End If
                    Case Keys.LineFeed
                        ' REGISB: Check if WM_IME_CHAR needs to be treated the same.
                        If m.Msg = Interop.NativeMethods.WM_CHAR AndAlso ModifierKeys = Keys.Control Then
                            ' Ignore linefeed character when user hits Ctrl-Enter to commit the cell.
                            Return True
                        End If
                    Case Keys.A
                        If m.Msg = Interop.NativeMethods.WM_KEYDOWN AndAlso ModifierKeys = Keys.Control Then
                            SelectAll()
                            Return True
                        End If
                End Select
                Return MyBase.ProcessKeyEventArgs(m)
            End Function
        End Class

        ''' <summary>
        ''' Custom button that alternates between ... and combobox drop down
        ''' </summary>
        Private Class ComboBoxDotDotDotButton
            Inherits Button

            ' Valid paint styles
            Public Enum PaintStyles
                DotDotDot = 0
                DropDown = 1
            End Enum

            Private Const DotDotDotString As String = "..."

            ' Current style to draw
            Private _paintStyle As PaintStyles = PaintStyles.DotDotDot

            ' Hot or not
            Private _drawHot As Boolean

            ''' <summary>
            ''' Do we want to look like a browse button or like a 
            ''' combobox dropdown?
            ''' </summary>
            Public Property PaintStyle As PaintStyles
                Get
                    Return _paintStyle
                End Get
                Set
                    Select Case value
                        Case PaintStyles.DotDotDot, PaintStyles.DropDown
                            ' Everything is cool
                        Case Else
                            Throw CreateArgumentException(NameOf(value))
                    End Select
                    _paintStyle = value
                    Invalidate()
                End Set
            End Property

            ''' <summary>
            ''' Keep track on when the mouse is over us
            ''' </summary>
            ''' <param name="e"></param>
            Protected Overrides Sub OnMouseEnter(e As EventArgs)
                _drawHot = True
                Invalidate()
                MyBase.OnMouseEnter(e)
            End Sub

            ''' <summary>
            ''' Keep track on when the mouse is over us
            ''' </summary>
            ''' <param name="e"></param>
            Protected Overrides Sub OnMouseLeave(e As EventArgs)
                _drawHot = False
                Invalidate()
                MyBase.OnMouseLeave(e)
            End Sub

            ''' <summary>
            ''' Custom paint ... or combobox drop down...
            ''' </summary>
            ''' <param name="pevent"></param>
            Protected Overrides Sub OnPaint(pevent As PaintEventArgs)
                MyBase.OnPaint(pevent)
                Select Case PaintStyle
                    Case PaintStyles.DotDotDot
                        Dim drawRect As Rectangle = ClientRectangle
                        drawRect.Offset(FlatAppearance.BorderSize, 0)
                        TextRenderer.DrawText(pevent.Graphics, DotDotDotString, Font, drawRect, ForeColor)
                    Case PaintStyles.DropDown
                        If ComboBoxRenderer.IsSupported Then
                            Dim drawstyle As VisualStyles.ComboBoxState
                            If _drawHot Then
                                drawstyle = VisualStyles.ComboBoxState.Hot
                            Else
                                drawstyle = VisualStyles.ComboBoxState.Normal
                            End If
                            ComboBoxRenderer.DrawDropDownButton(pevent.Graphics, ClientRectangle, drawstyle)
                        Else
                            ControlPaint.DrawComboButton(pevent.Graphics, ClientRectangle, ButtonState.Normal)
                        End If
                End Select
            End Sub
        End Class

    End Class
End Namespace
