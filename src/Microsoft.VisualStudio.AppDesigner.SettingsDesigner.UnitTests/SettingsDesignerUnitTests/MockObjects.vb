Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Mockisar

    Public Class MockITypeResolutionService
        Implements System.ComponentModel.Design.ITypeResolutionService

        Private m_knownTypes() As Type
        Private m_fallBackToLoadedTypes As Boolean

        Public Sub New(ByVal fallBackToLoadedTypes As Boolean, ByVal ParamArray types() As System.Type)
            m_knownTypes = types
            m_fallBackToLoadedTypes = fallBackToLoadedTypes
        End Sub

        Private Function ITypeResolutionService_GetAssembly(ByVal name As System.Reflection.AssemblyName) As System.Reflection.Assembly Implements System.ComponentModel.Design.ITypeResolutionService.GetAssembly
            Throw New NotImplementedException()
        End Function

        Private Function ITypeResolutionService_GetAssembly(ByVal name As System.Reflection.AssemblyName, ByVal throwOnError As Boolean) As System.Reflection.Assembly Implements System.ComponentModel.Design.ITypeResolutionService.GetAssembly
            Throw New NotImplementedException()
        End Function

        Private Function ITypeResolutionService_GetPathOfAssembly(ByVal name As System.Reflection.AssemblyName) As String Implements System.ComponentModel.Design.ITypeResolutionService.GetPathOfAssembly
            Throw New NotImplementedException()
        End Function

        Private Function ITypeResolutionService_GetType(ByVal name As String) As System.Type Implements System.ComponentModel.Design.ITypeResolutionService.GetType
            Return ITypeResolutionService_GetType(name, False, False)
        End Function

        Private Function ITypeResolutionService_GetType(ByVal name As String, ByVal throwOnError As Boolean) As System.Type Implements System.ComponentModel.Design.ITypeResolutionService.GetType
            Dim t As Type = ITypeResolutionService_GetType(name)
            If t Is Nothing Then
                Throw New ArgumentException()
            Else
                Return t
            End If
        End Function

        Private Function ITypeResolutionService_GetType(ByVal name As String, ByVal throwOnError As Boolean, ByVal ignoreCase As Boolean) As System.Type Implements System.ComponentModel.Design.ITypeResolutionService.GetType
            Dim comparison As StringComparison
            If ignoreCase Then
                comparison = StringComparison.OrdinalIgnoreCase
            Else
                comparison = StringComparison.Ordinal
            End If
            For Each t As Type In m_knownTypes
                If String.Equals(t.FullName, name, comparison) Then
                    Return t
                End If
            Next

            If m_fallBackToLoadedTypes Then
                Dim mockDiscoveryService As New MockITypeDiscoveryService(True)
                For Each t As Type In mockDiscoveryService.GetTypes(GetType(System.Object), False)
                    If String.Equals(t.FullName, name, comparison) Then
                        Return t
                    End If
                Next
            End If
            Return Nothing
        End Function

        Private Sub ReferenceAssembly(ByVal name As System.Reflection.AssemblyName) Implements System.ComponentModel.Design.ITypeResolutionService.ReferenceAssembly
            Throw New NotImplementedException()
        End Sub
    End Class

    Public Class MockITypeDiscoveryService
        Implements System.ComponentModel.Design.ITypeDiscoveryService

        Private m_types() As System.Type

        Public Sub New(ByVal includeAllTypesInCurrentAppDomain As Boolean, ByVal ParamArray types() As System.Type)
            Dim allTypes As New System.Collections.Generic.Dictionary(Of System.Type, Object)
            If includeAllTypesInCurrentAppDomain Then
                For Each a As System.Reflection.Assembly In AppDomain.CurrentDomain.GetAssemblies()
                    Try
                        For Each t As System.Type In a.GetTypes()
                            allTypes(t) = Nothing
                        Next
                    Catch ex As Exception
                    End Try
                Next
            End If
            For Each t As System.Type In types
                allTypes(t) = Nothing
            Next
            ReDim m_types(allTypes.Keys.Count - 1)
            allTypes.Keys.CopyTo(m_types, 0)
        End Sub

        Friend Function GetTypes(ByVal baseType As System.Type, ByVal excludeGlobalTypes As Boolean) As System.Collections.ICollection Implements System.ComponentModel.Design.ITypeDiscoveryService.GetTypes
            Dim result As New System.Collections.Generic.List(Of System.Type)

            For Each t As System.Type In m_types
                If baseType.IsAssignableFrom(t) Then
                    result.Add(t)
                End If
            Next
            Return result
        End Function
    End Class

    Public Class MockSiteWithName
        Implements System.ComponentModel.ISite

        Private m_name As String
        Private m_container As System.ComponentModel.IContainer
        Private m_component As System.ComponentModel.IComponent

        Public Sub New(ByVal component As System.ComponentModel.IComponent, ByVal name As String, ByVal container As System.ComponentModel.IContainer)
            m_component = component
            m_component.Site = Me
            m_container = container
            m_container.Add(component, name)
            m_name = name
        End Sub

        Public ReadOnly Property Component() As System.ComponentModel.IComponent Implements System.ComponentModel.ISite.Component
            Get
                Return m_component
            End Get
        End Property

        Public ReadOnly Property Container() As System.ComponentModel.IContainer Implements System.ComponentModel.ISite.Container
            Get
                Return m_container
            End Get
        End Property

        Public ReadOnly Property DesignMode() As Boolean Implements System.ComponentModel.ISite.DesignMode
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Property Name() As String Implements System.ComponentModel.ISite.Name
            Get
                Return m_name
            End Get
            Set(ByVal value As String)
                If Me.Container IsNot Nothing Then
                    Dim existingComponent As ComponentModel.IComponent = Me.Container.Components(value)
                    If existingComponent IsNot Nothing AndAlso existingComponent IsNot Me.Component Then
                        Throw New ArgumentException()
                    End If
                End If
                m_name = value
            End Set
        End Property

        Public Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            If serviceType Is GetType(System.ComponentModel.Design.IComponentChangeService) Then
                Return DirectCast(Me.Container, System.ComponentModel.Design.IComponentChangeService)
            ElseIf serviceType Is GetType(System.ComponentModel.Design.IDesignerHost) Then
                Return New MockDesignerHost
            ElseIf String.Equals(serviceType.FullName, "Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider", StringComparison.Ordinal) Then
                Return Nothing
            End If
            Throw New NotImplementedException()
        End Function
    End Class

    Friend Class MockContainer
        Implements System.ComponentModel.IContainer, System.ComponentModel.Design.IComponentChangeService

        Private m_components As New System.Collections.Generic.List(Of ComponentModel.IComponent)
        Private m_componentNames As New System.Collections.Generic.Dictionary(Of String, ComponentModel.IComponent)

        Public Sub Add(ByVal component As System.ComponentModel.IComponent) Implements System.ComponentModel.IContainer.Add
            m_components.Add(component)
            RaiseEvent ComponentAdded(Me, New System.ComponentModel.Design.ComponentEventArgs(component))
        End Sub

        Public Sub Add(ByVal component As System.ComponentModel.IComponent, ByVal name As String) Implements System.ComponentModel.IContainer.Add
            m_componentNames(name) = component
            Add(component)
        End Sub

        Public ReadOnly Property Components() As System.ComponentModel.ComponentCollection Implements System.ComponentModel.IContainer.Components
            Get
                Return New System.ComponentModel.ComponentCollection(m_components.ToArray())
            End Get
        End Property

        Public Sub Remove(ByVal component As System.ComponentModel.IComponent) Implements System.ComponentModel.IContainer.Remove
            If m_components.Contains(component) Then
                Try
                    RaiseEvent ComponentRemoving(Me, New System.ComponentModel.Design.ComponentEventArgs(component))
                    m_components.Remove(component)
                    Dim keyToRemove As String = Nothing
                    For Each key As String In m_componentNames.Keys
                        If Object.ReferenceEquals(m_componentNames(key), component) Then
                            keyToRemove = key
                            Exit For
                        End If
                    Next
                    If keyToRemove IsNot Nothing Then
                        m_componentNames.Remove(keyToRemove)
                    End If
                    RaiseEvent ComponentRemoved(Me, New ComponentModel.Design.ComponentEventArgs(component))
                Catch ex As Exception
                    ' Remove cancelled!
                End Try
            End If
        End Sub

        Private disposedValue As Boolean = False        ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: free unmanaged resources when explicitly called
                End If

                ' TODO: free shared unmanaged resources
            End If
            Me.disposedValue = True
        End Sub

#Region " IDisposable Support "
        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

        Public Event ComponentAdded(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentAdded

        Public Event ComponentAdding(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentAdding

        Public Event ComponentChanged(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangedEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentChanged

        Public Event ComponentChanging(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangingEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentChanging

        Public Event ComponentRemoved(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentRemoved

        Public Event ComponentRemoving(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentRemoving

        Public Event ComponentRename(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentRenameEventArgs) Implements System.ComponentModel.Design.IComponentChangeService.ComponentRename

        Public Sub OnComponentChanged(ByVal component As Object, ByVal member As System.ComponentModel.MemberDescriptor, ByVal oldValue As Object, ByVal newValue As Object) Implements System.ComponentModel.Design.IComponentChangeService.OnComponentChanged
            Dim e As New System.ComponentModel.Design.ComponentChangedEventArgs(component, member, oldValue, newValue)
            RaiseEvent ComponentChanged(Me, e)
        End Sub

        Public Sub OnComponentChanging(ByVal component As Object, ByVal member As System.ComponentModel.MemberDescriptor) Implements System.ComponentModel.Design.IComponentChangeService.OnComponentChanging
            Dim e As New System.ComponentModel.Design.ComponentChangingEventArgs(component, member)
            RaiseEvent ComponentChanging(Me, e)
        End Sub
    End Class

    Friend Class MockDesignerHost
        Implements System.ComponentModel.Design.IDesignerHost

        Public Sub Activate() Implements System.ComponentModel.Design.IDesignerHost.Activate
            Throw New NotImplementedException()
        End Sub

        Public Event Activated(ByVal sender As Object, ByVal e As System.EventArgs) Implements System.ComponentModel.Design.IDesignerHost.Activated

        Public ReadOnly Property Container() As System.ComponentModel.IContainer Implements System.ComponentModel.Design.IDesignerHost.Container
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Function CreateComponent(ByVal componentClass As System.Type) As System.ComponentModel.IComponent Implements System.ComponentModel.Design.IDesignerHost.CreateComponent
            Throw New NotImplementedException()
        End Function

        Public Function CreateComponent(ByVal componentClass As System.Type, ByVal name As String) As System.ComponentModel.IComponent Implements System.ComponentModel.Design.IDesignerHost.CreateComponent
            Throw New NotImplementedException()
        End Function

        Public Function CreateTransaction() As System.ComponentModel.Design.DesignerTransaction Implements System.ComponentModel.Design.IDesignerHost.CreateTransaction
            Return New MockDesignerTransaction()
        End Function

        Public Function CreateTransaction(ByVal description As String) As System.ComponentModel.Design.DesignerTransaction Implements System.ComponentModel.Design.IDesignerHost.CreateTransaction
            Return New MockDesignerTransaction()
        End Function

        Public Event Deactivated(ByVal sender As Object, ByVal e As System.EventArgs) Implements System.ComponentModel.Design.IDesignerHost.Deactivated

        Public Sub DestroyComponent(ByVal component As System.ComponentModel.IComponent) Implements System.ComponentModel.Design.IDesignerHost.DestroyComponent
            Throw New NotImplementedException()
        End Sub

        Public Function GetDesigner(ByVal component As System.ComponentModel.IComponent) As System.ComponentModel.Design.IDesigner Implements System.ComponentModel.Design.IDesignerHost.GetDesigner
            Throw New NotImplementedException()
        End Function

        Public Function GetType1(ByVal typeName As String) As System.Type Implements System.ComponentModel.Design.IDesignerHost.GetType
            Throw New NotImplementedException()
        End Function

        Public ReadOnly Property InTransaction() As Boolean Implements System.ComponentModel.Design.IDesignerHost.InTransaction
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event LoadComplete(ByVal sender As Object, ByVal e As System.EventArgs) Implements System.ComponentModel.Design.IDesignerHost.LoadComplete

        Public ReadOnly Property Loading() As Boolean Implements System.ComponentModel.Design.IDesignerHost.Loading
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property RootComponent() As System.ComponentModel.IComponent Implements System.ComponentModel.Design.IDesignerHost.RootComponent
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property RootComponentClassName() As String Implements System.ComponentModel.Design.IDesignerHost.RootComponentClassName
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event TransactionClosed(ByVal sender As Object, ByVal e As System.ComponentModel.Design.DesignerTransactionCloseEventArgs) Implements System.ComponentModel.Design.IDesignerHost.TransactionClosed

        Public Event TransactionClosing(ByVal sender As Object, ByVal e As System.ComponentModel.Design.DesignerTransactionCloseEventArgs) Implements System.ComponentModel.Design.IDesignerHost.TransactionClosing

        Public ReadOnly Property TransactionDescription() As String Implements System.ComponentModel.Design.IDesignerHost.TransactionDescription
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event TransactionOpened(ByVal sender As Object, ByVal e As System.EventArgs) Implements System.ComponentModel.Design.IDesignerHost.TransactionOpened

        Public Event TransactionOpening(ByVal sender As Object, ByVal e As System.EventArgs) Implements System.ComponentModel.Design.IDesignerHost.TransactionOpening

        Public Sub AddService(ByVal serviceType As System.Type, ByVal serviceInstance As Object) Implements System.ComponentModel.Design.IServiceContainer.AddService
            Throw New NotImplementedException()
        End Sub

        Public Sub AddService(ByVal serviceType As System.Type, ByVal serviceInstance As Object, ByVal promote As Boolean) Implements System.ComponentModel.Design.IServiceContainer.AddService
            Throw New NotImplementedException()
        End Sub

        Public Sub AddService(ByVal serviceType As System.Type, ByVal callback As System.ComponentModel.Design.ServiceCreatorCallback) Implements System.ComponentModel.Design.IServiceContainer.AddService
            Throw New NotImplementedException()
        End Sub

        Public Sub AddService(ByVal serviceType As System.Type, ByVal callback As System.ComponentModel.Design.ServiceCreatorCallback, ByVal promote As Boolean) Implements System.ComponentModel.Design.IServiceContainer.AddService
            Throw New NotImplementedException()
        End Sub

        Public Sub RemoveService(ByVal serviceType As System.Type) Implements System.ComponentModel.Design.IServiceContainer.RemoveService
            Throw New NotImplementedException()
        End Sub

        Public Sub RemoveService(ByVal serviceType As System.Type, ByVal promote As Boolean) Implements System.ComponentModel.Design.IServiceContainer.RemoveService
            Throw New NotImplementedException()
        End Sub

        Public Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class MockDesignerTransaction
        Inherits System.ComponentModel.Design.DesignerTransaction

        Protected Overrides Sub OnCancel()
        End Sub

        Protected Overrides Sub OnCommit()
        End Sub
    End Class

    Public Class MockServiceProvider
        Implements IServiceProvider

        Private m_services As New System.Collections.Generic.Dictionary(Of System.Type, Object)

        Public Sub AddService(ByVal type As System.Type, ByVal service As Object)
            m_services(type) = service
        End Sub

        Public Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            Dim service As Object = Nothing
            m_services.TryGetValue(serviceType, service)
            Return service
        End Function
    End Class

    Public Class MockUIService
        Implements System.Windows.Forms.Design.IUIService

        Public Function CanShowComponentEditor(ByVal component As Object) As Boolean Implements System.Windows.Forms.Design.IUIService.CanShowComponentEditor
            Throw New NotImplementedException
        End Function

        Public Function GetDialogOwnerWindow() As System.Windows.Forms.IWin32Window Implements System.Windows.Forms.Design.IUIService.GetDialogOwnerWindow
            Throw New NotImplementedException
        End Function

        Public Sub SetUIDirty() Implements System.Windows.Forms.Design.IUIService.SetUIDirty
            Throw New NotImplementedException
        End Sub

        Public Function ShowComponentEditor(ByVal component As Object, ByVal parent As System.Windows.Forms.IWin32Window) As Boolean Implements System.Windows.Forms.Design.IUIService.ShowComponentEditor
            Throw New NotImplementedException
        End Function

        Public Function ShowDialog(ByVal form As System.Windows.Forms.Form) As System.Windows.Forms.DialogResult Implements System.Windows.Forms.Design.IUIService.ShowDialog
            Throw New NotImplementedException
        End Function

        Public Sub ShowError(ByVal message As String) Implements System.Windows.Forms.Design.IUIService.ShowError
            Throw New NotImplementedException
        End Sub

        Public Sub ShowError(ByVal ex As System.Exception) Implements System.Windows.Forms.Design.IUIService.ShowError
            Throw New NotImplementedException
        End Sub

        Public Sub ShowError(ByVal ex As System.Exception, ByVal message As String) Implements System.Windows.Forms.Design.IUIService.ShowError
            Throw New NotImplementedException
        End Sub

        Public Sub ShowMessage(ByVal message As String) Implements System.Windows.Forms.Design.IUIService.ShowMessage
            Throw New NotImplementedException
        End Sub

        Public Sub ShowMessage(ByVal message As String, ByVal caption As String) Implements System.Windows.Forms.Design.IUIService.ShowMessage
            Throw New NotImplementedException
        End Sub

        Public Function ShowMessage(ByVal message As String, ByVal caption As String, ByVal buttons As System.Windows.Forms.MessageBoxButtons) As System.Windows.Forms.DialogResult Implements System.Windows.Forms.Design.IUIService.ShowMessage
            Throw New NotImplementedException
        End Function

        Public Function ShowToolWindow(ByVal toolWindow As System.Guid) As Boolean Implements System.Windows.Forms.Design.IUIService.ShowToolWindow
            Throw New NotImplementedException
        End Function

        Public ReadOnly Property Styles() As System.Collections.IDictionary Implements System.Windows.Forms.Design.IUIService.Styles
            Get
                Throw New NotImplementedException
            End Get
        End Property
    End Class

    Public Class MockIVsUiShell
        Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell

        Public Function AddNewBFNavigationItem(ByVal pWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame, ByVal bstrData As String, ByVal punk As Object, ByVal fReplaceCurrent As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.AddNewBFNavigationItem
            Throw New NotImplementedException
        End Function

        Public Function CenterDialogOnWindow(ByVal hwndDialog As System.IntPtr, ByVal hwndParent As System.IntPtr) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.CenterDialogOnWindow
            Throw New NotImplementedException
        End Function

        Public Function CreateDocumentWindow(ByVal grfCDW As UInteger, ByVal pszMkDocument As String, ByVal pUIH As Microsoft.VisualStudio.Shell.Interop.IVsUIHierarchy, ByVal itemid As UInteger, ByVal punkDocView As System.IntPtr, ByVal punkDocData As System.IntPtr, ByRef rguidEditorType As System.Guid, ByVal pszPhysicalView As String, ByRef rguidCmdUI As System.Guid, ByVal psp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider, ByVal pszOwnerCaption As String, ByVal pszEditorCaption As String, ByVal pfDefaultPosition() As Integer, ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.CreateDocumentWindow
            Throw New NotImplementedException
        End Function

        Public Function CreateToolWindow(ByVal grfCTW As UInteger, ByVal dwToolWindowId As UInteger, ByVal punkTool As Object, ByRef rclsidTool As System.Guid, ByRef rguidPersistenceSlot As System.Guid, ByRef rguidAutoActivate As System.Guid, ByVal psp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider, ByVal pszCaption As String, ByVal pfDefaultPosition() As Integer, ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.CreateToolWindow
            Throw New NotImplementedException
        End Function

        Public Function EnableModeless(ByVal fEnable As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.EnableModeless
            Throw New NotImplementedException
        End Function

        Public Function FindToolWindow(ByVal grfFTW As UInteger, ByRef rguidPersistenceSlot As System.Guid, ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.FindToolWindow
            Throw New NotImplementedException
        End Function

        Public Function FindToolWindowEx(ByVal grfFTW As UInteger, ByRef rguidPersistenceSlot As System.Guid, ByVal dwToolWinId As UInteger, ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.FindToolWindowEx
            Throw New NotImplementedException
        End Function

        Public Function GetAppName(ByRef pbstrAppName As String) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetAppName
            pbstrAppName = "Microsoft Visual Studio"
            Return 0
        End Function

        Public Function GetCurrentBFNavigationItem(ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame, ByRef pbstrData As String, ByRef ppunk As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetCurrentBFNavigationItem
            Throw New NotImplementedException
        End Function

        Public Function GetDialogOwnerHwnd(ByRef phwnd As System.IntPtr) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetDialogOwnerHwnd
            Throw New NotImplementedException
        End Function

        Public Function GetDirectoryViaBrowseDlg(ByVal pBrowse() As Microsoft.VisualStudio.Shell.Interop.VSBROWSEINFOW) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetDirectoryViaBrowseDlg
            Throw New NotImplementedException
        End Function

        Public Function GetDocumentWindowEnum(ByRef ppenum As Microsoft.VisualStudio.Shell.Interop.IEnumWindowFrames) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetDocumentWindowEnum
            Throw New NotImplementedException
        End Function

        Public Function GetErrorInfo(ByRef pbstrErrText As String) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetErrorInfo
            Throw New NotImplementedException
        End Function

        Public Function GetNextBFNavigationItem(ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame, ByRef pbstrData As String, ByRef ppunk As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetNextBFNavigationItem
            Throw New NotImplementedException
        End Function

        Public Function GetOpenFileNameViaDlg(ByVal pOpenFileName() As Microsoft.VisualStudio.Shell.Interop.VSOPENFILENAMEW) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetOpenFileNameViaDlg
            Throw New NotImplementedException
        End Function

        Public Function GetPreviousBFNavigationItem(ByRef ppWindowFrame As Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame, ByRef pbstrData As String, ByRef ppunk As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetPreviousBFNavigationItem
            Throw New NotImplementedException
        End Function

        Public Function GetSaveFileNameViaDlg(ByVal pSaveFileName() As Microsoft.VisualStudio.Shell.Interop.VSSAVEFILENAMEW) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetSaveFileNameViaDlg
            Throw New NotImplementedException
        End Function

        Public Function GetToolWindowEnum(ByRef ppenum As Microsoft.VisualStudio.Shell.Interop.IEnumWindowFrames) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetToolWindowEnum
            Throw New NotImplementedException
        End Function

        Public Function GetURLViaDlg(ByVal pszDlgTitle As String, ByVal pszStaticLabel As String, ByVal pszHelpTopic As String, ByRef pbstrURL As String) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetURLViaDlg
            Throw New NotImplementedException
        End Function

        Public Function GetVSSysColor(ByVal dwSysColIndex As Microsoft.VisualStudio.Shell.Interop.VSSYSCOLOR, ByRef pdwRGBval As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.GetVSSysColor
            Throw New NotImplementedException
        End Function

        Public Function OnModeChange(ByVal dbgmodeNew As Microsoft.VisualStudio.Shell.Interop.DBGMODE) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.OnModeChange
            Throw New NotImplementedException
        End Function

        Public Function PostExecCommand(ByRef pguidCmdGroup As System.Guid, ByVal nCmdID As UInteger, ByVal nCmdexecopt As UInteger, ByRef pvaIn As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.PostExecCommand
            Throw New NotImplementedException
        End Function

        Public Function PostSetFocusMenuCommand(ByRef pguidCmdGroup As System.Guid, ByVal nCmdID As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.PostSetFocusMenuCommand
            Throw New NotImplementedException
        End Function

        Public Function RefreshPropertyBrowser(ByVal dispid As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.RefreshPropertyBrowser
            Throw New NotImplementedException
        End Function

        Public Function RemoveAdjacentBFNavigationItem(ByVal rdDir As Microsoft.VisualStudio.Shell.Interop.RemoveBFDirection) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.RemoveAdjacentBFNavigationItem
            Throw New NotImplementedException
        End Function

        Public Function RemoveCurrentNavigationDupes(ByVal rdDir As Microsoft.VisualStudio.Shell.Interop.RemoveBFDirection) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.RemoveCurrentNavigationDupes
            Throw New NotImplementedException
        End Function

        Public Function ReportErrorInfo(ByVal hr As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.ReportErrorInfo
            Throw New NotImplementedException
        End Function

        Public Function SaveDocDataToFile(ByVal grfSave As Microsoft.VisualStudio.Shell.Interop.VSSAVEFLAGS, ByVal pPersistFile As Object, ByVal pszUntitledPath As String, ByRef pbstrDocumentNew As String, ByRef pfCanceled As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SaveDocDataToFile
            Throw New NotImplementedException
        End Function

        Public Function SetErrorInfo(ByVal hr As Integer, ByVal pszDescription As String, ByVal dwReserved As UInteger, ByVal pszHelpKeyword As String, ByVal pszSource As String) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetErrorInfo
            Throw New NotImplementedException
        End Function

        Public Function SetForegroundWindow() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetForegroundWindow
            Throw New NotImplementedException
        End Function

        Public Function SetMRUComboText(ByRef pguidCmdGroup As System.Guid, ByVal dwCmdID As UInteger, ByVal lpszText As String, ByVal fAddToList As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetMRUComboText
            Throw New NotImplementedException
        End Function

        Public Function SetMRUComboTextW(ByVal pguidCmdGroup() As System.Guid, ByVal dwCmdID As UInteger, ByVal pwszText As String, ByVal fAddToList As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetMRUComboTextW
            Throw New NotImplementedException
        End Function

        Public Function SetToolbarVisibleInFullScreen(ByVal pguidCmdGroup() As System.Guid, ByVal dwToolbarId As UInteger, ByVal fVisibleInFullScreen As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetToolbarVisibleInFullScreen
            Throw New NotImplementedException
        End Function

        Public Function SetupToolbar(ByVal hwnd As System.IntPtr, ByVal ptwt As Microsoft.VisualStudio.Shell.Interop.IVsToolWindowToolbar, ByRef pptwth As Microsoft.VisualStudio.Shell.Interop.IVsToolWindowToolbarHost) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetupToolbar
            Throw New NotImplementedException
        End Function

        Public Function SetWaitCursor() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.SetWaitCursor
            Throw New NotImplementedException
        End Function

        Public Function ShowContextMenu(ByVal dwCompRole As UInteger, ByRef rclsidActive As System.Guid, ByVal nMenuId As Integer, ByVal pos() As Microsoft.VisualStudio.Shell.Interop.POINTS, ByVal pCmdTrgtActive As Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.ShowContextMenu
            Throw New NotImplementedException
        End Function

        Public Function ShowMessageBox(ByVal dwCompRole As UInteger, ByRef rclsidComp As System.Guid, ByVal pszTitle As String, ByVal pszText As String, ByVal pszHelpFile As String, ByVal dwHelpContextID As UInteger, ByVal msgbtn As Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON, ByVal msgdefbtn As Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON, ByVal msgicon As Microsoft.VisualStudio.Shell.Interop.OLEMSGICON, ByVal fSysAlert As Integer, ByRef pnResult As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.ShowMessageBox
            Return 0
        End Function

        Public Function TranslateAcceleratorAsACmd(ByVal pMsg() As Microsoft.VisualStudio.OLE.Interop.MSG) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.TranslateAcceleratorAsACmd
            Throw New NotImplementedException
        End Function

        Public Function UpdateCommandUI(ByVal fImmediateUpdate As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.UpdateCommandUI
            Throw New NotImplementedException
        End Function

        Public Function UpdateDocDataIsDirtyFeedback(ByVal docCookie As UInteger, ByVal fDirty As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsUIShell.UpdateDocDataIsDirtyFeedback
            Throw New NotImplementedException
        End Function
    End Class

    Public Class MockIVsHierarchy
        Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy

        'Private Fake_project As ProjectFake
        'Private Fake_dte As DTEFake
        'Private Fake_projectProperties As ProjectPropertiesFake
        'Private Fake_vsProjectSpecialFiles As VsProjectSpecialFiles
        'Private Fake_supportedMyApplicationTypes As String = "WindowsApp;WindowsClassLib;CommandLineApp;WindowsService;WebControl"
        'Private Dictionary<uint, ProjectItem> Fake_projectItems = new Dictionary<uint, ProjectItem>();
        Private Class MockIServiceProvider
            Implements Microsoft.VisualStudio.OLE.Interop.IServiceProvider

            Public Function QueryService(ByRef guidService As Guid, ByRef riid As Guid, ByRef ppvObject As IntPtr) As Integer Implements OLE.Interop.IServiceProvider.QueryService
                ppvObject = New IntPtr
                Return VSConstants.S_OK
            End Function
        End Class

        Public Function AdviseHierarchyEvents(pEventSink As Microsoft.VisualStudio.Shell.Interop.IVsHierarchyEvents, ByRef pdwCookie As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.AdviseHierarchyEvents
            Throw New NotImplementedException()
        End Function

        Public Function Close() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Close
            Throw New NotImplementedException()
        End Function

        Public Function GetCanonicalName(itemid As UInteger, ByRef pbstrName As String) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.GetCanonicalName
            Throw New NotImplementedException()
        End Function

        Public Function GetGuidProperty(itemid As UInteger, propid As Integer, ByRef pguid As Guid) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.GetGuidProperty
            Throw New NotImplementedException()
        End Function

        Public Function GetNestedHierarchy(itemid As UInteger, ByRef iidHierarchyNested As Guid, ByRef ppHierarchyNested As IntPtr, ByRef pitemidNested As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.GetNestedHierarchy
            Throw New NotImplementedException()
        End Function

        Public Function GetProperty(itemid As UInteger, propid As Integer, ByRef pvar As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.GetProperty
            If __VSHPROPID4.VSHPROPID_TargetFrameworkMoniker = propid Then
                pvar = ".NET Framework, Version=4.0"
            Else
                pvar = Nothing
            End If
            Return VSConstants.S_OK
        End Function

        Public Function GetSite(ByRef ppSP As Microsoft.VisualStudio.OLE.Interop.IServiceProvider) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.GetSite
            ppSP = New MockIServiceProvider
            Return VSConstants.S_OK
        End Function

        Public Function ParseCanonicalName(pszName As String, ByRef pitemid As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.ParseCanonicalName
            Throw New NotImplementedException()
        End Function

        Public Function QueryClose(ByRef pfCanClose As Integer) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.QueryClose
            Throw New NotImplementedException()
        End Function

        Public Function SetGuidProperty(itemid As UInteger, propid As Integer, ByRef rguid As Guid) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.SetGuidProperty
            Throw New NotImplementedException()
        End Function

        Public Function SetProperty(itemid As UInteger, propid As Integer, var As Object) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.SetProperty
            Throw New NotImplementedException()
        End Function

        Public Function SetSite(psp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.SetSite
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseHierarchyEvents(dwCookie As UInteger) As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.UnadviseHierarchyEvents
            Throw New NotImplementedException()
        End Function

        Public Function Unused0() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Unused0
            Throw New NotImplementedException()
        End Function

        Public Function Unused1() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Unused1
            Throw New NotImplementedException()
        End Function

        Public Function Unused2() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Unused2
            Throw New NotImplementedException()
        End Function

        Public Function Unused3() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Unused3
            Throw New NotImplementedException()
        End Function

        Public Function Unused4() As Integer Implements Microsoft.VisualStudio.Shell.Interop.IVsHierarchy.Unused4
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace

Namespace TestHelperClasses

    Public Class ComponentChangeRecorder

        Private m_componentChangeService As System.ComponentModel.Design.IComponentChangeService

        Public Enum ChangeType
            Changing
            Changed
            Removed
            Added
        End Enum

        Private m_expectedEvents As New System.Collections.Generic.Queue(Of String)

        Public Sub New(ByVal changeService As System.ComponentModel.Design.IComponentChangeService)
            m_componentChangeService = changeService
            AddHandler changeService.ComponentChanging, AddressOf Me.ComponentChangingHandler
            AddHandler changeService.ComponentChanged, AddressOf Me.ComponentChangedHandler
        End Sub

        Public Sub AddComponentChanging(ByVal typeName As String, ByVal memberName As String)
            m_expectedEvents.Enqueue(String.Format(ComponentChangingFormatString, ChangeType.Changing, typeName, memberName))
        End Sub

        Public Sub AddComponentChanged(ByVal typeName As String, ByVal memberName As String, ByVal oldValue As Object, ByVal newValue As Object)
            m_expectedEvents.Enqueue(String.Format(ComponentChangedFormatString, ChangeType.Changed, typeName, memberName, oldValue, newValue))
        End Sub

        Public ReadOnly Property IsDone() As Boolean
            Get
                Return m_expectedEvents.Count = 0
            End Get
        End Property

        Private ReadOnly Property ComponentChangingFormatString() As String
            Get
                Return "{0}-{1}-{2}"
            End Get
        End Property

        Private ReadOnly Property ComponentChangedFormatString() As String
            Get
                Return "{0}-{1}-{2}-{3}-{4}"
            End Get
        End Property

        Private Sub ComponentChangingHandler(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangingEventArgs)
            Dim expectedEvent As String = m_expectedEvents.Dequeue()
            Dim actualEvent As String = String.Format(ComponentChangingFormatString, ChangeType.Changing, e.Component.GetType().FullName, e.Member.Name)
            If Not String.Equals(expectedEvent, actualEvent, StringComparison.Ordinal) Then
                Throw New InvalidOperationException()
            End If
        End Sub

        Private Sub ComponentChangedHandler(ByVal sender As Object, ByVal e As System.ComponentModel.Design.ComponentChangedEventArgs)
            Dim expectedEvent As String = m_expectedEvents.Dequeue()
            Dim actualEvent As String = String.Format(ComponentChangedFormatString, ChangeType.Changed, e.Component.GetType().FullName, e.Member.Name, e.OldValue, e.NewValue)
            If Not String.Equals(expectedEvent, actualEvent, StringComparison.Ordinal) Then
                Throw New InvalidOperationException()
            End If
        End Sub


    End Class

End Namespace
