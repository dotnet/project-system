' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Drawing
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports IOleDataObject = Microsoft.VisualStudio.OLE.Interop.IDataObject

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework

    ''' <summary>
    ''' This class provides a Window pane provider service that can
    '''   create a DesignerWindowPaneBase
    ''' This allows us to have more control of the WindowPane
    ''' </summary>
    Public Class DeferrableWindowPaneProviderServiceBase
        Inherits WindowPaneProviderService

        ' True if the toolbox should be supported
        Private ReadOnly _supportToolbox As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="provider"></param>
        ''' <param name="SupportToolbox"></param>
        Public Sub New(provider As IServiceProvider, SupportToolbox As Boolean)
            MyBase.New(provider)
            _supportToolbox = SupportToolbox
        End Sub

        Public Overrides Function CreateWindowPane(surface As DesignSurface) As DesignerWindowPane
            Return New DesignerWindowPaneBase(surface, _supportToolbox)
        End Function

        ''' <summary>
        ''' Simple DesignerWindowPane
        ''' The main reason for using a custom window pane as opposed to the WinformsWindowPane
        ''' that we would get for "free" is to allow us to receive IVsWindowPaneCommit.
        ''' 
        ''' </summary>
        Public Class DesignerWindowPaneBase
            Inherits DesignerWindowPane
            Implements IVsWindowPaneCommit

            Private _undoEngine As OleUndoEngine
            Private _undoCursor As Cursor

            Private _view As TopLevelControl       ' a TopLevel control is required to handle SystemEvent correctly when it is hosted inside a native window
            ' However, as the comments below, we can not use a Form here. We will have to create a customized control here.
            Private _loadError As Boolean
            Private _host As IDesignerHost

            ' True if toolbox support is to be enabled for this window pane
            Private ReadOnly _supportToolbox As Boolean

            ''' <summary>
            ''' Creates a new WinformsWindowPane.
            ''' </summary>
            ''' <param name="surface"></param>
            ''' <param name="SupportToolbox"></param>
            Public Sub New(surface As DesignSurface, SupportToolbox As Boolean)
                MyBase.New(surface)

                _supportToolbox = SupportToolbox

                ' Create our view control and hook its focus event.
                ' Do not be tempted to create a container control here
                ' and use it for focus management!  It will steal key
                ' events from the shell and break things.
                '
                _view = New TopLevelControl()
                AddHandler _view.GotFocus, AddressOf OnViewFocus
                _view.BackColor = SystemColors.Window

                'For debugging purposes
                _view.Name = "DesignerWindowPaneBase View"
                _view.Text = "DesignerWindowPaneBase View"

                _host = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
                If _host IsNot Nothing AndAlso Not _host.Loading Then
                    PopulateView()
                End If

                AddHandler surface.Loaded, AddressOf OnLoaded
                AddHandler surface.Unloading, AddressOf OnSurfaceUnloading
                AddHandler surface.Unloaded, AddressOf OnSurfaceUnloaded

            End Sub

            ''' <summary>
            ''' Returns the view control for the window pane.
            ''' </summary>
            Protected ReadOnly Property View As Control
                Get
                    Return _view
                End Get
            End Property

            ''' <summary>
            '''     This method is called when Visual Studio needs to
            '''     evaluate which toolbox items should be enabled.  The
            '''     default implementation searches the service provider
            '''     for IVsToolboxUser and delegates.  If IVsToolboxUser
            '''     cannot be found this will search the service provider for
            '''     IToolboxService and call IsSupported.
            ''' </summary>
            ''' <remarks>
            ''' We override this so we can disable toolbox support.
            ''' </remarks>
            Protected Overrides Function GetToolboxItemSupported(toolboxItem As IOleDataObject) As Boolean
                If Not _supportToolbox Then
                    'PERF: NOTE: If we don't need toolbox support, we simply return False here for all toolbox items (faster)
                    Return False
                End If

                'Otherwise, let the base class do its normal thing...
                Return MyBase.GetToolboxItemSupported(toolboxItem)
            End Function

            ''' <summary>
            ''' Retrieves our view.
            ''' </summary>
            Public Overrides ReadOnly Property Window As IWin32Window
                Get
                    ' This should always happen, but in case we never
                    ' got a load event we check.  We might not receive
                    ' a load event if a bad event handler threw before
                    ' we got invoked.
                    '
                    If _view IsNot Nothing AndAlso _view.Controls.Count = 0 Then
                        PopulateView()
                    End If
                    Return _view
                End Get
            End Property

            ''' <summary>
            ''' Called to disable OLE undo.
            ''' </summary>
            Private Sub DisableUndo()
                If _undoEngine IsNot Nothing Then

                    Dim c As IServiceContainer = DirectCast(GetService(GetType(IServiceContainer)), IServiceContainer)

                    If c IsNot Nothing Then
                        c.RemoveService(GetType(UndoEngine))
                    End If

                    _undoEngine.Dispose()
                    RemoveHandler _undoEngine.Undoing, AddressOf OnUndoing
                    RemoveHandler _undoEngine.Undone, AddressOf OnUndone
                    _undoEngine = Nothing
                End If
            End Sub

            ''' <summary>
            ''' Called when our view is disposed.
            ''' </summary>
            ''' <param name="disposing"></param>
            Protected Overrides Sub Dispose(disposing As Boolean)

                Dim disposedView As Control = _view

                Try
                    If disposing Then
                        ' The base class will try to dispose our view if
                        ' it exists.  We want to take care of that here after the
                        ' surface is disposed so the design surface can have a
                        ' chance to systematically tear down controls and components.
                        ' So set the view to null here, but remember it in
                        ' disposedView.  After we're done calling base.Dispose()
                        ' will take care of our own stuff.
                        '
                        _view = Nothing
                        DisableUndo()
                        Dim ds As DesignSurface = Surface
                        If ds IsNot Nothing Then
                            RemoveHandler ds.Loaded, AddressOf OnLoaded
                            RemoveHandler ds.Unloading, AddressOf OnSurfaceUnloading
                            RemoveHandler ds.Unloaded, AddressOf OnSurfaceUnloaded
                        End If
                    End If

                    MyBase.Dispose(disposing)
                Finally
                    If disposing AndAlso disposedView IsNot Nothing Then
                        RemoveHandler disposedView.GotFocus, AddressOf OnViewFocus
                        disposedView.Dispose()
                    End If
                End Try
            End Sub

            ''' <summary>
            ''' Called to enable OLE undo.
            ''' </summary>
            Private Sub EnableUndo()

                Debug.Assert(_undoEngine Is Nothing, "EnableUndo should only be called once.  Call DisableUndo before calling this again.")

                ' Undo requires that IDesignerSerializationService and
                ' IOleUndoManager are both present.  If they're not,
                ' don't hook up undo because it will throw anyway.
                '
                If GetService(GetType(ComponentSerializationService)) IsNot Nothing Then
                    _undoEngine = New OleUndoEngine(Surface)
                    AddHandler _undoEngine.Undoing, AddressOf OnUndoing
                    AddHandler _undoEngine.Undone, AddressOf OnUndone
                    Dim c As IServiceContainer = DirectCast(GetService(GetType(IServiceContainer)), IServiceContainer)
                    If c IsNot Nothing Then
                        c.AddService(GetType(UndoEngine), _undoEngine)
                    End If
                End If
            End Sub

            ''' <summary>
            ''' We override this to enable / disable undo.  The undo engine
            ''' should be disabled if our view is cached for later.
            ''' </summary>
            Protected Overrides Sub OnClose()
                DisableUndo()
                MyBase.OnClose()
            End Sub

            ''' <summary>
            ''' We override this to enable / disable undo.  The undo engine
            ''' should be disabled if our view is cached for later.
            ''' </summary>
            Protected Overrides Sub OnCreate()
                MyBase.OnCreate()

                _host = DirectCast(GetService(GetType(IDesignerHost)), IDesignerHost)
                If _host IsNot Nothing AndAlso Not _host.Loading Then
                    EnableUndo()
                End If

                ' make sure scrollbars in the pane are not themed
                Dim frame As IVsWindowFrame = TryCast(GetService(GetType(IVsWindowFrame)), IVsWindowFrame)
                If frame IsNot Nothing Then
                    frame.SetProperty(__VSFPROPID5.VSFPROPID_NativeScrollbarThemeMode, __VSNativeScrollbarThemeMode.NSTM_None)
                End If
            End Sub

            ''' <summary>
            ''' Called when the surface finishes loading.  Here we fish the view
            ''' out of the surface and also handle the white screen of darn.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnLoaded(sender As Object, e As LoadedEventArgs)
                PopulateView()
                EnableUndo()
                'ChangeFormEditorCaption()
            End Sub

            ''' <summary>
            ''' Called when the surface unloads.  During unload we disable
            ''' the undo engine until we have successfully reloaded.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnSurfaceUnloading(sender As Object, e As EventArgs)
                DisableUndo()
            End Sub

            ''' <summary>
            '''     Called when the surface has completed its unload.  If our view
            '''     was populated with controls from the designer then the view
            '''     should be empty now.  But, if it was populated with error
            '''     information then it could still have the error control on it,
            '''     in which case we should dispose it.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnSurfaceUnloaded(sender As Object, e As EventArgs)
                If _view IsNot Nothing AndAlso _view.Controls.Count > 0 Then
                    Dim ctrl(_view.Controls.Count - 1) As Control
                    _view.Controls.CopyTo(ctrl, 0)
                    For Each c As Control In ctrl
                        c.Dispose()
                    Next
                End If
            End Sub

            ''' <summary>
            ''' Called when an undo action is about to happen.  We freeze painting here.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnUndoing(sender As Object, e As EventArgs)
                If _view IsNot Nothing AndAlso _view.IsHandleCreated Then
                    NativeMethods.SendMessage(New HandleRef(_view, _view.Handle), NativeMethods.WM_SETREDRAW, 0, 0)
                    _undoCursor = Cursor.Current
                    Cursor.Current = Cursors.WaitCursor
                End If
            End Sub

            ''' <summary>
            ''' Called when an undo action is done.  We unfreeze painting here.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnUndone(sender As Object, e As EventArgs)
                If _view IsNot Nothing AndAlso _view.IsHandleCreated Then
                    NativeMethods.SendMessage(New HandleRef(_view, _view.Handle), NativeMethods.WM_SETREDRAW, 1, 0)
                    _view.Invalidate(True)
                    Cursor.Current = _undoCursor
                    _undoCursor = Nothing
                End If
            End Sub

            ''' <summary>
            ''' Our view always hands focus to its child.  
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub OnViewFocus(sender As Object, e As EventArgs)
                Common.Switches.TracePDFocus(TraceLevel.Warning, "DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase.m_View.OnGotFocus (OnViewFocus)")
                If _view IsNot Nothing AndAlso _view.Controls.Count > 0 Then
                    'The view's first child should be the designer root view.  Since
                    '  our m_View is simply a Control and not a container control, we
                    '  need to forward focus manually to the designer's root view, otherwise
                    '  it stays unhelpfully on the window pane control.
                    Dim DesignerRootView As Control = _view.Controls(0)
                    Debug.Assert(DesignerRootView IsNot Nothing)

                    Common.Switches.TracePDFocus(TraceLevel.Warning, "  ...setting focus to view's first child [should be designer root view]: """ & DesignerRootView.Name & """" & " (type """ & DesignerRootView.GetType.Name & """)")
                    DesignerRootView.Focus()
#If DEBUG Then
                    Dim h As IntPtr = NativeMethods.GetFocus()
                    If Not DesignerRootView.CanFocus Then
                        Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... root view isn't currently focusable.")
                    End If
                    Common.Switches.TracePDFocus(TraceLevel.Warning, "  ... Focus ended up on HWND = " & Hex(h.ToInt32))
#End If
                End If
            End Sub

            ''' <summary>
            ''' This takes our control UI and populates it with the
            '''    design surface.  If there was an error encountered
            '''    it will display the WSOD.
            ''' </summary>
            Private Sub PopulateView()

                _view.SuspendLayout()
                Dim viewChild As Control

                Try
                    'This will be the View for the root designer
                    viewChild = TryCast(Surface.View, Control)
                    _loadError = False
                Catch loadError As Exception

                    Do While TypeOf loadError Is TargetInvocationException AndAlso loadError.InnerException IsNot Nothing
                        loadError = loadError.InnerException
                    Loop

                    Dim message As String = loadError.Message

                    If message Is Nothing OrElse message.Length = 0 Then
                        message = loadError.ToString()
                    End If

                    Dim errors As ArrayList = New ArrayList From {
                        message
                    }
                    viewChild = New ErrorControl(errors)
                    _loadError = True
                End Try

                If viewChild Is Nothing Then
                    Dim er As String = My.Resources.Designer.DFX_WindowPane_UnknownError
                    Dim errors As ArrayList = New ArrayList From {
                        er
                    }
                    viewChild = New ErrorControl(errors)
                End If

                ' PopulateView may be called multiple times for the same
                ' view - we have to make sure that the new view isn't already
                ' hosted before disposing & replacing the view control...
                ' (VsWhidbey 468042)
                If Not _view.Controls.Contains(viewChild) Then
                    'Dispose of previous controls before clearing them
                    Dim ctrl(_view.Controls.Count - 1) As Control
                    _view.Controls.CopyTo(ctrl, 0)
                    For Each c As Control In ctrl
                        c.Dispose()
                    Next
                    _view.Controls.Clear()

                    viewChild.SuspendLayout()
                    viewChild.Dock = DockStyle.Fill
                    _view.BackColor = viewChild.BackColor
                    viewChild.ResumeLayout(False)
                    _view.Controls.Add(viewChild)
                End If
                _view.ResumeLayout()
            End Sub

#Region "IVsWindowPaneCommit"
            ''' <summary>
            ''' Allow us to commit pending changes before we receive a command such as Undo or when
            ''' the user presses F5
            ''' 
            ''' This implementation will check the Surface's view, and if it implements the IVsWindowPaneCommit
            ''' it will forward the command to the view.
            ''' </summary>
            ''' <param name="pfCommitFailed"></param>
            Public Function IVsWindowPaneCommit_CommitPendingEdit(ByRef pfCommitFailed As Integer) As Integer Implements IVsWindowPaneCommit.CommitPendingEdit
                Dim viewAsIVsWindowPaneCommit As IVsWindowPaneCommit = Nothing
                If Not _loadError AndAlso Surface IsNot Nothing Then
                    viewAsIVsWindowPaneCommit = TryCast(Surface.View, IVsWindowPaneCommit)
                End If
                If viewAsIVsWindowPaneCommit IsNot Nothing Then
                    ' Let the view helper handle this....
                    viewAsIVsWindowPaneCommit.CommitPendingEdit(pfCommitFailed)
                Else
                    ' We did *not* fail - set flag to FALSE
                    pfCommitFailed = 0
                End If
            End Function

#End Region

            ''' <summary>
            ''' A toplevel control is needed to handle SystemEvents. When the control is hosted in a native window, there will be no parent WinForm control.
            ''' Form could handle this correctly. However, for some reason, we couldn't use it here. We have to create a customized class to make a non-form topLevel control.
            ''' </summary>
            Private Class TopLevelControl
                Inherits Control

                ''' <summary>
                ''' Constructor
                ''' </summary>
                Public Sub New()
                    MyBase.New()

                    SetTopLevel(True)
                End Sub

                ''' <summary>
                ''' Overrides CreateParams to make sure it is created as a child window
                ''' </summary>
                Protected Overrides ReadOnly Property CreateParams As CreateParams
                    Get
                        Dim cp As CreateParams = MyBase.CreateParams()
                        cp.Style = cp.Style Or Constants.WS_CHILD Or Constants.WS_CLIPSIBLINGS
                        Return cp
                    End Get
                End Property
            End Class

        End Class

    End Class

End Namespace

