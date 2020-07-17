' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Imports Microsoft.VisualStudio.Shell.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public Class ProjectDesignerTabControl
        Inherits ContainerControl

        'A list of all buttons currently contained by the control
        Private ReadOnly _buttonCollection As New List(Of ProjectDesignerTabButton)

        Private ReadOnly _renderer As ProjectDesignerTabRenderer 'The renderer to use for painting.  May not be Nothing.
        Private _selectedItem As ProjectDesignerTabButton ' Currently-selected item.  May be Nothing.
        Private _hoverItem As ProjectDesignerTabButton ' Currently-hovered item.  May be Nothing.
        Private _hostingPanel As Panel

        'The overflow button for displaying tabs which can't currently fit
        Public WithEvents OverflowButton As Button

        'The overflow menu that gets displayed when the overflow button is pressed
        Private ReadOnly _overflowMenu As New ContextMenuStrip
        Private ReadOnly _overflowTooltip As New ToolTip

        'Backs up the ServiceProvider property
        Private _serviceProvider As IServiceProvider

        'Backs up the VsUIShellService property
        Private _uiShellService As IVsUIShell

        'Backs up the VsUIShell2Service property
        Private _uiShell2Service As IVsUIShell2

        'Backs up the VsUIShell5Service property
        Private _uiShell5Service As IVsUIShell5

        ''' <summary>
        '''  Listen for font/color changes from the shell
        ''' </summary>
        Private WithEvents _broadcastMessageEventsHelper As Common.ShellUtil.BroadcastMessageEventsHelper

        Public Event ThemeChanged(sender As Object, args As EventArgs)

#Region " Component Designer generated code "

        Public Sub New()
            _renderer = New ProjectDesignerTabRenderer(Me)

            SuspendLayout()
            Try
                ' This call is required by the Component Designer.
                InitializeComponent()

                Initialize()
            Finally
                ResumeLayout()
            End Try
        End Sub 'New

        'Control override dispose to clean up the component list.
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
                If _broadcastMessageEventsHelper IsNot Nothing Then
                    _broadcastMessageEventsHelper.Dispose()
                    _broadcastMessageEventsHelper = Nothing
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub 'Dispose

        'Required by the Control Designer
        Private _components As System.ComponentModel.IContainer

        ' NOTE: The following procedure is required by the Component Designer
        ' It can be modified using the Component Designer.  Do not modify it
        ' using the code editor.
        '<System.Diagnostics.DebuggerNonUserCode()> 
        Private Sub InitializeComponent()
            SuspendLayout()
            _components = New System.ComponentModel.Container

            'No scrollbars
            AutoScroll = False

            ResumeLayout()
        End Sub

#End Region

        ''' <summary>
        ''' Initialization
        ''' </summary>
        Private Sub Initialize()
            _hostingPanel = New Panel With {
                .Visible = True,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Bottom Or AnchorStyles.Right,
                .AutoScroll = True,
                .Text = "HostingPanel", 'For debugging
                .AccessibleName = My.Resources.Designer.APPDES_HostingPanelName
            }

            'Add any initialization after the InitializeComponent() call
            '
            Name = "DesignerTabControl"
            Padding = New Padding(0)
            Size = New Size(144, 754)
            TabIndex = 0
            DoubleBuffered = True
            Controls.Add(_hostingPanel)

            SetUpOverflowButton()
        End Sub 'InitTabInfo

        ''' <summary>
        ''' Create the tab overflow button.
        ''' </summary>
        Private Sub SetUpOverflowButton()
            'Note: the renderer will position the button, so we don't need to.
            OverflowButton = New ImageButton("Microsoft.VisualStudio.Editors.ApplicationDesigner.OverflowImage", Color.Lime)
            With OverflowButton
                .Name = "OverflowButton"
                .Text = ""
                .AccessibleName = My.Resources.Designer.APPDES_OverflowButton_AccessibilityName
                .Size = New Size(18, 18)
                .Visible = False 'Don't show it until we need it
                _overflowTooltip.SetToolTip(OverflowButton, My.Resources.Designer.APPDES_OverflowButton_Tooltip)

            End With
            Controls.Add(OverflowButton)
        End Sub

        ''' <summary>
        ''' The service provider to use when querying for services related to hosting this control
        '''   instead of the Visual Studio shell.
        ''' Default is Nothing.  If not set, then behavior will be independent of the Visual Studio
        '''   shell (e.g., colors will default to system or fallback colors instead of using the
        '''   shell's color service). 
        ''' </summary>
        Public Property ServiceProvider As IServiceProvider
            Get
                Return _serviceProvider
            End Get
            Set
                _serviceProvider = value
                _renderer.ServiceProvider = value

                If _serviceProvider IsNot Nothing Then
                    OnGotServiceProvider()
                End If
            End Set
        End Property

        Protected ReadOnly Property VsUIShellService As IVsUIShell
            Get
                If _uiShellService Is Nothing Then
                    If Common.VBPackageInstance IsNot Nothing Then
                        _uiShellService = TryCast(Common.VBPackageInstance.GetService(GetType(IVsUIShell)), IVsUIShell)
                    ElseIf ServiceProvider IsNot Nothing Then
                        _uiShellService = TryCast(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                    End If
                End If

                Return _uiShellService
            End Get
        End Property

        Protected ReadOnly Property VsUIShell2Service As IVsUIShell2
            Get
                If _uiShell2Service Is Nothing Then
                    Dim VsUIShell = VsUIShellService

                    If VsUIShell IsNot Nothing Then
                        _uiShell2Service = TryCast(VsUIShell, IVsUIShell2)
                    End If
                End If

                Return _uiShell2Service
            End Get
        End Property

        Protected ReadOnly Property VsUIShell5Service As IVsUIShell5
            Get
                If _uiShell5Service Is Nothing Then
                    Dim VsUIShell = VsUIShellService

                    If VsUIShell IsNot Nothing Then
                        _uiShell5Service = TryCast(VsUIShell, IVsUIShell5)
                    End If
                End If

                Return _uiShell5Service
            End Get
        End Property

        ''' <summary>
        ''' Called when a non-empty service provider is given to the control.
        ''' </summary>
        Private Sub OnGotServiceProvider()
            'We now should have access to the color provider service
            OnThemeChanged()

            If _broadcastMessageEventsHelper IsNot Nothing Then
                _broadcastMessageEventsHelper.Dispose()
                _broadcastMessageEventsHelper = Nothing
            End If
            If _serviceProvider IsNot Nothing Then
                _broadcastMessageEventsHelper = New Common.ShellUtil.BroadcastMessageEventsHelper(_serviceProvider)
            End If
        End Sub

        Protected Overridable Sub OnThemeChanged()
            'Update our themed colors
            _hostingPanel.BackColor = Common.ShellUtil.GetProjectDesignerThemeColor(VsUIShell5Service, "Background", __THEMEDCOLORTYPE.TCT_Background, SystemColors.Window)

            'Update our system colors
            Dim VsUIShell2 = VsUIShell2Service
            OverflowButton.FlatAppearance.BorderColor = Common.ShellUtil.GetColor(VsUIShell2, __VSSYSCOLOREX.VSCOLOR_COMMANDBAR_BORDER, SystemColors.WindowFrame)
            OverflowButton.FlatAppearance.MouseOverBackColor = Common.ShellUtil.GetColor(VsUIShell2, __VSSYSCOLOREX.VSCOLOR_COMMANDBAR_HOVER, SystemColors.Highlight)

            'Force the renderer to recreate its GDI objects
            _renderer.CreateGDIObjects(True)

            RaiseEvent ThemeChanged(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Returns an enumerable set of tab buttons
        ''' </summary>
        Public ReadOnly Property TabButtons As IEnumerable(Of ProjectDesignerTabButton)
            Get
                Return _buttonCollection
            End Get
        End Property

        ''' <summary>
        ''' Clears all the tab buttons off of the control
        ''' </summary>
        Public Sub ClearTabs()
            _buttonCollection.Clear()
            InvalidateLayout()
        End Sub

        ''' <summary>
        ''' Gets a tab button by index
        ''' </summary>
        ''' <param name="index"></param>
        Public Function GetTabButton(index As Integer) As ProjectDesignerTabButton
            Return _buttonCollection(index)
        End Function

        ''' <summary>
        ''' The number of tab buttons, including those not currently visible
        ''' </summary>
        Public ReadOnly Property TabButtonCount As Integer
            Get
                Return _buttonCollection.Count
            End Get
        End Property

        ''' <summary>
        ''' Get the panel that is used to host controls on the right-hand side
        ''' </summary>
        Public ReadOnly Property HostingPanel As Panel
            Get
                Return _hostingPanel
            End Get
        End Property

        ''' <summary>
        ''' Perform layout
        ''' </summary>
        ''' <param name="levent"></param>
        Protected Overrides Sub OnLayout(levent As LayoutEventArgs)
            Common.Switches.TracePDPerfBegin(levent, "ProjectDesignerTabControl.OnLayout()")

            _renderer.PerformLayout() 'This can affect the layout of other controls on this page
            MyBase.OnLayout(levent)

            Invalidate()
            Common.Switches.TracePDPerfEnd("ProjectDesignerTabControl.OnLayout()")
        End Sub 'OnLayout

        ''' <summary>
        ''' Causes the layout to be refreshed
        ''' </summary>
        Protected Sub InvalidateLayout()
            PerformLayout()
        End Sub

        ''' <summary>
        ''' Adds a new tab to the control
        ''' </summary>
        ''' <param name="Title">The user-friendly, localizable text for the tab that will be displayed.</param>
        ''' <param name="AutomationName">Non-localizable name to be used for QA automation.</param>
        Public Function AddTab(Title As String, AutomationName As String) As Integer
            SuspendLayout()
            Dim newIndex As Integer
            Try
                Dim Button As New ProjectDesignerTabButton With {
                    .Text = Title,
                    .Name = AutomationName
                }
                _buttonCollection.Add(Button)
                newIndex = _buttonCollection.Count - 1
                Controls.Add(Button)
                Button.SetIndex(newIndex)
                Button.Visible = True

                If SelectedItem Is Nothing Then
                    SelectedItem = Button
                End If
            Finally
                ResumeLayout()
            End Try

            Return newIndex
        End Function 'AddTab

        ''' <summary>
        ''' Tracks the last item for paint logic
        ''' </summary>
        Public ReadOnly Property HoverItem As ProjectDesignerTabButton
            Get
                Return _hoverItem
            End Get
        End Property

        ''' <summary>
        ''' Currently selected button
        ''' </summary>
        Public Property SelectedItem As ProjectDesignerTabButton
            Get
                Return _selectedItem
            End Get
            Set
                Dim oldSelectedItem As ProjectDesignerTabButton = Nothing

                If _selectedItem Is value Then
                    Return
                End If
                If _selectedItem IsNot Nothing Then
                    oldSelectedItem = _selectedItem
                    _selectedItem.Invalidate()
                End If
                _selectedItem = value

                'If the selected item was the hover item, then clear it
                If _hoverItem Is value Then
                    _hoverItem = Nothing
                End If
                If _selectedItem IsNot Nothing Then
                    _selectedItem.Visible = True 'Must be visible in order to properly get the focus
                    If _selectedItem.CanFocus AndAlso SelectedItem.TabStop Then
                        Common.Switches.TracePDFocus(TraceLevel.Warning, "ProjectDesignerTabControl.set_SelectedItem - Setting focus to selected tab")
                        _selectedItem.Focus()
                    End If
                    _selectedItem.Invalidate()
                End If

                ' Fire state change event to notify the screen reader...
                If oldSelectedItem IsNot Nothing Then
                    CType(oldSelectedItem.AccessibilityObject, ControlAccessibleObject).NotifyClients(AccessibleEvents.StateChange)
                End If
                If _selectedItem IsNot Nothing Then
                    CType(_selectedItem.AccessibilityObject, ControlAccessibleObject).NotifyClients(AccessibleEvents.StateChange)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Currently selected button
        ''' </summary>
        Public Property SelectedIndex As Integer
            Get
                If _selectedItem Is Nothing Then
                    Return -1
                End If

                Return _selectedItem.ButtonIndex
            End Get
            Set
                If value = -1 Then
                    SelectedItem = Nothing
                Else
                    SelectedItem = _buttonCollection(value)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Keep painting from happening during WM_PAINT.  We'll paint everything during OnPaintBackground.
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
        End Sub

        ''' <summary>
        ''' Everything will paint in the background, except buttons which handle their own painting
        ''' </summary>
        ''' <param name="e"></param>
        Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
            Renderer.RenderBackground(e.Graphics)
        End Sub

        ''' <summary>
        ''' Occurs when a button is clicked.
        ''' </summary>
        ''' <param name="item">The tab button which has been clicked.</param>
        Public Overridable Sub OnItemClick(item As ProjectDesignerTabButton)
            OnItemClick(item, reactivatePage:=False)
        End Sub

        Public Overridable Sub OnItemClick(item As ProjectDesignerTabButton, reactivatePage As Boolean)
            SelectedItem = item
        End Sub

        Friend Overridable Sub SetControl(firstControl As Boolean)

        End Sub

        ''' <summary>
        ''' Occurs when the mouse enters a button's area
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        Public Sub OnItemEnter(e As EventArgs, item As ProjectDesignerTabButton)
            If _hoverItem IsNot item Then
                _hoverItem = item
                item.Invalidate()
            End If
        End Sub

        ''' <summary>
        ''' Occurs when the mouse leaves a button's area
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        Public Sub OnItemLeave(e As EventArgs, item As ProjectDesignerTabButton)
            If _hoverItem Is item Then
                _hoverItem = Nothing
                item.Invalidate()
            End If
        End Sub

        ''' <summary>
        ''' Occurs when a tab button gets focus
        ''' </summary>
        ''' <param name="e"></param>
        ''' <param name="item"></param>
        Public Overridable Sub OnItemGotFocus(e As EventArgs, item As ProjectDesignerTabButton)
        End Sub

        ''' <summary>
        ''' Create customized accessible object
        ''' </summary>
        Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
            Return New DesignerTabControlAccessibleObject(Me)
        End Function

        ''' <summary>
        ''' Retrieves the renderer used for this tab control
        ''' </summary>
        Public ReadOnly Property Renderer As ProjectDesignerTabRenderer
            Get
                Return _renderer
            End Get
        End Property

        ''' <summary>
        ''' Overflow button has been clicked.  Bring up menu of non-visible tab items.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OverflowButton_Click(sender As Object, e As EventArgs) Handles OverflowButton.Click
            'Set up to use VS colors
            If _serviceProvider IsNot Nothing Then
                Dim uiSvc As IUIService = DirectCast(_serviceProvider.GetService(GetType(IUIService)), IUIService)
                'Set up the menu font and toolstrip renderer
                If uiSvc IsNot Nothing Then
                    Dim Renderer As ToolStripProfessionalRenderer = DirectCast(uiSvc.Styles("VsRenderer"), ToolStripProfessionalRenderer)
                    If Renderer IsNot Nothing Then
                        _overflowMenu.Renderer = Renderer
                    End If

                    Dim NewFont As Font = DirectCast(uiSvc.Styles("DialogFont"), Font)
                    If NewFont IsNot Nothing Then
                        _overflowMenu.Font = NewFont
                    End If

                    Dim CommandBarTextActiveColor As Color = DirectCast(uiSvc.Styles("CommandBarTextActive"), Color)
                    _overflowMenu.ForeColor = CommandBarTextActiveColor

                    Dim CommandBarMenuBackgroundGradientEndColor As Color = DirectCast(uiSvc.Styles("CommandBarMenuBackgroundGradientEnd"), Color)
                    _overflowMenu.BackColor = CommandBarMenuBackgroundGradientEndColor

                End If
            End If

            'Remove old menu items and handlers
            For Each Item As ToolStripMenuItem In _overflowMenu.Items
                RemoveHandler Item.Click, AddressOf OverflowMenuItemClick
            Next
            _overflowMenu.Items.Clear()

            'Create a menu structure for the buttons, and let the user select from that.  We include in the overflow
            '  menu only buttons which are not currently visible in the available space.
            For Each button As ProjectDesignerTabButton In TabButtons
                If Not button.Visible Then
                    Dim MenuItem As New ToolStripMenuItem()
                    With MenuItem
                        .Text = button.TextWithDirtyIndicator
                        .Name = "Overflow_" & button.Name 'For automation - should not be localized
                        AddHandler .Click, AddressOf OverflowMenuItemClick
                        .Tag = button
                    End With
                    _overflowMenu.Items.Add(MenuItem)
                End If
            Next

            If _overflowMenu.Items.Count > 0 Then
                'Show the overflow menu
                Dim OverflowMenuDistanceFromButtonButtonLeft As Size = New Size(-2, 2)
                _overflowMenu.Show(Me,
                    OverflowButton.Left + OverflowMenuDistanceFromButtonButtonLeft.Width,
                    OverflowButton.Bottom + OverflowMenuDistanceFromButtonButtonLeft.Height)
            Else
                Debug.Fail("How did the overflow button get clicked if there are no items to show in the overflow area?")
            End If
        End Sub

        ''' <summary>
        ''' Happens when the user clicks on an entry in the overflow menu.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OverflowMenuItemClick(sender As Object, e As EventArgs)
            Dim MenuItem As ToolStripMenuItem = DirectCast(sender, ToolStripMenuItem)
            Dim Button As ProjectDesignerTabButton = DirectCast(MenuItem.Tag, ProjectDesignerTabButton)
            Debug.Assert(Button IsNot Nothing)
            If Button IsNot Nothing Then
                'Click it
                OnItemClick(Button)

                'We need to ensure that the selected button becomes and stays visible in the tabs now.
                '  We do that by setting it as the preferred button for the switchable slot.
                '(This must be done after OnItemClick, because otherwise the selected item will be
                '  wrong and the renderer gives preference to the selected item.)
                _renderer.PreferredButtonForSwitchableSlot = Button
            End If
        End Sub

        ''' <summary>
        ''' We've gotta tell the renderer whenever the system colors change...
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wparam"></param>
        ''' <param name="lparam"></param>
        Private Sub OnBroadcastMessageEventsHelperBroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr) Handles _broadcastMessageEventsHelper.BroadcastMessage
            Select Case msg
                Case AppDesInterop.Win32Constant.WM_PALETTECHANGED, AppDesInterop.Win32Constant.WM_SYSCOLORCHANGE, AppDesInterop.Win32Constant.WM_THEMECHANGED
                    OnThemeChanged()
            End Select
        End Sub

        '*************************************************
        '* Private Class ImageButton
        '*************************************************

        ''' <summary>
        ''' A button that has these characteristics:
        '''   a) contains a transparent image from resources
        '''   b) has flatstyle
        '''   c) shows a border only when the mouse hovers over it
        ''' </summary>
        Private Class ImageButton
            Inherits Button

            Public Sub New()
                'We don't want it to get focus.  Also, if we don't do this, it will have
                '  a border size too large when it does obtain focus (or thinks it does).  
                '  Setting TabStop=False isn't enough.
                SetStyle(ControlStyles.Selectable, False)

                FlatStyle = FlatStyle.Flat
                FlatAppearance.BorderSize = 0 'No border until the mouse is over it
                TabStop = True
                BackColor = Color.Transparent 'Need to let gradients show through the image when not hovered over
            End Sub

            Public Sub New(ImageResourceId As String, TransparentColor As Color)
                Me.New()

                'Get the image and make it transparent
                Dim Image As Image = Common.GetManifestBitmapTransparent(ImageResourceId, TransparentColor, GetType(ProjectDesignerTabControl).Assembly)
                MyBase.Image = Image
            End Sub

            ''' <summary>
            ''' Occurs when the mouse enters the button
            ''' </summary>
            ''' <param name="e"></param>
            Protected Overrides Sub OnMouseEnter(e As EventArgs)
                MyBase.OnMouseEnter(e)

                'No border unless the mouse is over the button
                FlatAppearance.BorderSize = 1
                BackColor = FlatAppearance.MouseOverBackColor
            End Sub

            ''' <summary>
            ''' Occurs when the mouse leaves the button
            ''' </summary>
            ''' <param name="e"></param>
            Protected Overrides Sub OnMouseLeave(e As EventArgs)
                MyBase.OnMouseLeave(e)

                'No border unless the mouse is over the button
                FlatAppearance.BorderSize = 0
                BackColor = Color.Transparent
            End Sub

        End Class

        '''<summary>
        ''' custom build accessible object class
        '''</summary>
        Private Class DesignerTabControlAccessibleObject
            Inherits ControlAccessibleObject

            ' button which this accessible object belongs to
            Private ReadOnly _tabControl As ProjectDesignerTabControl

            Public Sub New(owner As ProjectDesignerTabControl)
                MyBase.New(owner)
                _tabControl = owner
            End Sub

            ''' <summary>
            ''' Description
            ''' </summary>
            Public Overrides ReadOnly Property Description As String
                Get
                    Return My.Resources.Designer.APPDES_TabListDescription
                End Get
            End Property

            ''' <summary>
            ''' Role - it is a tab List
            ''' </summary>
            Public Overrides ReadOnly Property Role As AccessibleRole
                Get
                    Return AccessibleRole.PageTabList
                End Get
            End Property

            ''' <summary>
            ''' Value - the name of the active page
            ''' </summary>
            Public Overrides Property Value As String
                Get
                    If _tabControl.SelectedItem IsNot Nothing Then
                        Return _tabControl.SelectedItem.Text
                    Else
                        Return Nothing
                    End If
                End Get
                Set
                End Set
            End Property

        End Class

    End Class

End Namespace
