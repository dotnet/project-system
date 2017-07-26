' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Public Class ProjectDesignerTabButton
        Inherits Button

        Private _index As Integer
        Private _dirtyIndicator As Boolean
        Private _focusedFromKeyboardNav As Boolean


        Public Sub New()
            SetStyle(ControlStyles.SupportsTransparentBackColor, True)
            BackColor = Color.Transparent
            SetStyle(ControlStyles.Opaque Or ControlStyles.StandardClick, False)

            ' If the UserMouse style is set, the control does its own processing
            '   of mouse messages (keeps Control's WmMouseDown from calling into DefWndProc)
            SetStyle(ControlStyles.UserMouse Or ControlStyles.UserPaint, True)

            FlatStyle = FlatStyle.Flat

            'We need the tab buttons to be able to receive focus, so that we can 
            '  redirect focus back to the selected page when the shell is activated.
            SetStyle(ControlStyles.Selectable, True)
            TabStop = True
        End Sub 'New


        ''' <summary>
        ''' True if the dirty indicator should be display
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DirtyIndicator() As Boolean
            Get
                Return _dirtyIndicator
            End Get
            Set(value As Boolean)
                If value <> _dirtyIndicator Then
                    _dirtyIndicator = value
                    Invalidate()
                End If
            End Set
        End Property


        ''' <summary>
        ''' Returns the text of the tab button, with the dirty indicator if it is on.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property TextWithDirtyIndicator() As String
            Get
                'If the dirty indicator is on, append "*" to the text
                Dim ButtonText As String = Text
                If DirtyIndicator Then
                    ButtonText &= "*"
                End If

                Return ButtonText
            End Get
        End Property


        ''' <summary>
        ''' The location of the button.  Should not be changed directly except
        '''   by the tab control itself.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>

        Public Shadows Property Location() As Point
            Get
                Return MyBase.Location
            End Get
            Set(value As Point) 'Make inaccessible except to this assembly 'CONSIDER: this is non-CLS-compliant, should change if make control public
                MyBase.Location = value
            End Set
        End Property


        Public ReadOnly Property ButtonIndex() As Integer
            Get
                Return _index
            End Get
        End Property

        Public Sub SetIndex(index As Integer)
            _index = index
        End Sub


        Private ReadOnly Property ParentTabControl() As ProjectDesignerTabControl
            Get
                Return DirectCast(Parent, ProjectDesignerTabControl)
            End Get
        End Property


        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.Renderer.RenderButton(e.Graphics, Me, Me Is parent.SelectedItem, Me Is parent.HoverItem)
            End If
        End Sub


        '''<summary>
        '''</summary>
        '''<remarks> We need handle OnClick to make Accessiblity work... </remarks>
        Protected Overrides Sub OnClick(e As EventArgs)
            MyBase.OnClick(e)

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemClick(Me)
            End If
        End Sub


        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemEnter(e, Me)
            End If
        End Sub


        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemLeave(e, Me)
            End If
        End Sub

        Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
            Dim keyCode = keyData And Keys.KeyCode

            Select Case keyCode
                Case Keys.Enter
                    Dim parent As ProjectDesignerTabControl = ParentTabControl
                    If parent IsNot Nothing Then
                        parent.OnItemClick(Me, reactivatePage:=True)
                    End If
                Case Keys.Tab, Keys.Up, Keys.Down

                    If keyCode <> Keys.Tab OrElse (keyData And Keys.Control) <> Keys.Control Then                                   ' Don't process if Ctrl+Tab, but do Process for Ctrl+Up and Ctrl+Down
                        Dim parent As ProjectDesignerTabControl = ParentTabControl
                        If parent IsNot Nothing Then
                            Dim nextIndex As Int32 = ButtonIndex
                            If (keyCode = Keys.Tab AndAlso (keyData And Keys.Shift) = Keys.Shift) OrElse keyCode = Keys.Up Then     ' Process if Shift+Tab or Up
                                nextIndex -= 1
                                If nextIndex < 0 Then
                                    nextIndex = parent.TabButtonCount - 1
                                End If
                            Else
                                nextIndex += 1
                                If nextIndex >= parent.TabButtonCount Then
                                    nextIndex = 0
                                End If
                            End If
                            Dim nextButton = parent.GetTabButton(nextIndex)
                            nextButton.Focus()
                            nextButton.FocusedFromKeyboardNav = True
                            Return True
                        End If
                    End If
            End Select
            Return MyBase.ProcessDialogKey(keyData)
        End Function

        Friend ReadOnly Property DrawFocusCues As Boolean
            Get
                Return Focused And FocusedFromKeyboardNav
            End Get
        End Property

        Friend Property FocusedFromKeyboardNav As Boolean
            Get
                Return _focusedFromKeyboardNav
            End Get
            Set(value As Boolean)
                _focusedFromKeyboardNav = value
            End Set
        End Property

        Protected Overrides Sub OnGotFocus(e As EventArgs)
            Common.Switches.TracePDFocus(TraceLevel.Warning, "ProjectDesignerTabButton.OnGotFocus - forwarding to parent")
            MyBase.OnGotFocus(e)

            FocusedFromKeyboardNav = False

            Dim parent As ProjectDesignerTabControl = ParentTabControl
            If parent IsNot Nothing Then
                parent.OnItemGotFocus(e, Me)
            End If
            Invalidate()
        End Sub

        Protected Overrides Sub OnLostFocus(e As EventArgs)
            FocusedFromKeyboardNav = False
        End Sub


        ''' <summary>
        ''' Create customized accessible object
        ''' </summary>
        Protected Overrides Function CreateAccessibilityInstance() As AccessibleObject
            Return New DesignerTabButtonAccessibleObject(Me)
        End Function

        ''' <summary>
        ''' accessible state
        ''' </summary>
        Public ReadOnly Property AccessibleState() As AccessibleStates
            Get
                Dim parent As ProjectDesignerTabControl = ParentTabControl
                If parent IsNot Nothing AndAlso Me Is parent.SelectedItem Then
                    Return AccessibleStates.Selectable Or AccessibleStates.Selected
                Else
                    Return AccessibleStates.Selectable
                End If
            End Get
        End Property

        '''<summary>
        ''' custom build accessible object class
        '''</summary>
        Private Class DesignerTabButtonAccessibleObject
            Inherits ButtonBaseAccessibleObject

            ' button which this accessible object belongs to
            Private _button As ProjectDesignerTabButton

            Public Sub New(owner As ProjectDesignerTabButton)
                MyBase.New(owner)
                _button = owner
            End Sub

            ''' <summary>
            ''' accessible state
            ''' </summary>
            Public Overrides ReadOnly Property State() As AccessibleStates
                Get
                    Return MyBase.State Or _button.AccessibleState
                End Get
            End Property

            ''' <summary>
            ''' Default action name.
            ''' </summary>
            Public Overrides ReadOnly Property DefaultAction() As String
                Get
                    Return My.Resources.Designer.APPDES_TabButtonDefaultAction
                End Get
            End Property

            ''' <summary>
            ''' Role - it is a tab page
            ''' </summary>
            Public Overrides ReadOnly Property Role() As AccessibleRole
                Get
                    Return AccessibleRole.PageTab
                End Get
            End Property

            ''' <summary>
            ''' Do the default action - select the tab
            ''' </summary>
            Public Overrides Sub DoDefaultAction()
                _button.PerformClick()
            End Sub

        End Class

    End Class

End Namespace
