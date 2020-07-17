' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Drawing
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.WCFReference.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class ReferencePropPage
        Inherits PropPageUserControlBase
        Implements VSLangProj._dispReferencesEvents
        Implements VSLangProj._dispImportsEvents
        Implements ISelectionContainer
        Implements IVsWCFReferenceEvents

        Private Const REFCOLUMN_NAME As Integer = 0
        Private Const REFCOLUMN_TYPE As Integer = 1
        Private Const REFCOLUMN_VERSION As Integer = 2
        Private Const REFCOLUMN_COPYLOCAL As Integer = 3
        Private Const REFCOLUMN_PATH As Integer = 4
        Private Const REFCOLUMN_MAX As Integer = 4

        Friend WithEvents AddUserImportButton As Button
        Friend WithEvents UpdateUserImportButton As Button
        Friend WithEvents UserImportTextBox As TextBox

        Private _referencesEventsCookie As NativeMethods.ConnectionPointCookie
        Private _importsEventsCookie As NativeMethods.ConnectionPointCookie
        Private _updatingReferences As Boolean
        Private _updatingImportList As Boolean

        Private _designerHost As IDesignerHost
        Private _trackSelection As ITrackSelection
        Private _holdSelectionChange As Integer

        Private _delayUpdatingItems As Queue
        Private _columnWidthUpdated As Boolean

        Private _ignoreImportEvent As Boolean
        Friend WithEvents addRemoveButtonsTableLayoutPanel As TableLayoutPanel
        Friend WithEvents referenceButtonsTableLayoutPanel As TableLayoutPanel
        Friend WithEvents ReferencePageSplitContainer As SplitContainer
        Friend WithEvents addUserImportTableLayoutPanel As TableLayoutPanel
        Private _needRefreshImportList As Boolean
        Private _importListSelectedItem As String
        Private _hidingImportListSelectedItem As Boolean

        ' helper object to sort the reference list
        Private ReadOnly _referenceSorter As ListViewComparer

        Private _referenceGroupManager As IVsWCFReferenceManager

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Scale buttons
            addSplitButton.Size = DpiAwareness.LogicalToDeviceSize(Handle, addSplitButton.Size)
            RemoveReference.Size = DpiAwareness.LogicalToDeviceSize(Handle, RemoveReference.Size)
            UpdateReferences.Size = DpiAwareness.LogicalToDeviceSize(Handle, UpdateReferences.Size)

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
            PageRequiresScaling = False

            'support sorting
            _referenceSorter = New ListViewComparer()
            ReferenceList.ListViewItemSorter = _referenceSorter
            _referenceSorter.Sorting = SortOrder.Ascending
            ReferenceList.Sorting = SortOrder.Ascending
        End Sub

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        Protected Overrides Sub CleanupCOMReferences()

            UnadviseReferencesEvents()
            UnadviseWebReferencesEvents()
            UnadviseServiceReferencesEvents()
            UnadviseImportsEvents()

            MyBase.CleanupCOMReferences()
        End Sub

        Protected Overrides Sub EnableAllControls(enabled As Boolean)
            MyBase.EnableAllControls(enabled)

            ReferenceList.Enabled = enabled
            addSplitButton.Enabled = enabled
            RemoveReference.Enabled = enabled
            UpdateReferences.Enabled = enabled
            UnusedReferences.Enabled = enabled
            GetPropertyControlData("ImportList").EnableControls(enabled)
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                        New PropertyControlData(1, "ImportList", ImportList, AddressOf ImportListSet, AddressOf ImportListGet, ControlDataFlags.UserPersisted)
                        }
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' The designer host of this page
        ''' NOTE: we currently get the designer host from the propertyPageDesignerView, it is a workaround. The right solution should be the parent page pass in the right serviceProvider when it creates/initializes this page
        ''' </summary>
        Private ReadOnly Property DesignerHost As IDesignerHost
            Get
                If _designerHost Is Nothing Then
                    Dim designerView As PropPageDesigner.PropPageDesignerView = FindPropPageDesignerView()
                    Debug.Assert(designerView IsNot Nothing, "why we can not find the designerView")
                    If designerView IsNot Nothing Then
                        _designerHost = designerView.DesignerHost
                        Debug.Assert(_designerHost IsNot Nothing, "why we can not find DesignerHost")
                    End If
                End If
                Return _designerHost
            End Get
        End Property

        ''' <summary>
        ''' Property to return the selected-item of the ImportList which is smart about whether or
        ''' not we are hiding the selection currently to work around the by-design CheckedListBox
        ''' behavior of visually looking like it has focus when it really doesn't.
        ''' </summary>
        Private ReadOnly Property ImportListSelectedItem As String
            Get
                Debug.Assert(ImportList.SelectedItems.Count <= 1, "the ImportList is not set up to support multiple selection")

                If ImportList.SelectedItems.Count = 1 Then
                    Return DirectCast(ImportList.SelectedItem, String)
                ElseIf _importListSelectedItem IsNot Nothing Then
                    Return _importListSelectedItem
                End If

                Return String.Empty

            End Get
        End Property

        ''' <summary>
        ''' ITrackSelection -- we are using this service to push objects to the propertyPage.
        '''  We should get this service from DesignerHost, but not other service provider. Each designer has its own ITrackSelection
        ''' </summary>
        Private ReadOnly Property TrackSelection As ITrackSelection
            Get
                If _trackSelection Is Nothing Then
                    Dim host As IDesignerHost = DesignerHost
                    If host IsNot Nothing Then
                        _trackSelection = CType(host.GetService(GetType(ITrackSelection)), ITrackSelection)
                        Debug.Assert(_trackSelection IsNot Nothing, "Why we can not find ITrackSelection Service")
                    End If
                End If
                Return _trackSelection
            End Get
        End Property

        Public Overrides Function ReadUserDefinedProperty(PropertyName As String, ByRef Value As Object) As Boolean
            If PropertyName = "ImportList" Then
                Value = GetCurrentImports()
                Return True
            End If
            Return False
        End Function

        Public Overrides Function WriteUserDefinedProperty(PropertyName As String, Value As Object) As Boolean
            If PropertyName = "ImportList" Then
                Debug.Assert(TypeOf Value Is String(), "Invalid value type")
                SaveImportedNamespaces(DirectCast(Value, String()))
                Return True
            End If
            Return False
        End Function

        Public Overrides Function GetUserDefinedPropertyDescriptor(PropertyName As String) As PropertyDescriptor
            If PropertyName = "ImportList" Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String()))
            End If

            Debug.Fail("Unexpected user-defined property descriptor name")
            Return Nothing
        End Function

        ''' <summary>
        ''' Called when the control layout code wants to know the Preferred size of this page
        ''' </summary>
        ''' <param name="proposedSize"></param>
        ''' <remarks>We need implement this, because split panel doesn't support AutoSize well</remarks>
        Public Overrides Function GetPreferredSize(proposedSize As Size) As Size
            Dim preferredSize As Size = MyBase.GetPreferredSize(proposedSize)
            Dim referenceAreaPreferredSize As Size = Size.Empty
            Dim importsAreaPreferredSize As Size = Size.Empty

            If ReferencePageTableLayoutPanel IsNot Nothing Then
                referenceAreaPreferredSize = ReferencePageTableLayoutPanel.GetPreferredSize(New Size(proposedSize.Width, ReferencePageTableLayoutPanel.Height))
            End If
            If addUserImportTableLayoutPanel IsNot Nothing Then
                importsAreaPreferredSize = addUserImportTableLayoutPanel.GetPreferredSize(New Size(proposedSize.Width, importsAreaPreferredSize.Height))
            End If

            ' NOTE: 6 is 2 times of the margin we used. The exactly number is not important, because it actually does not make any difference on the page.
            Return New Size(Math.Max(preferredSize.Width, Math.Max(referenceAreaPreferredSize.Width, importsAreaPreferredSize.Width) + 6),
                    Math.Max(preferredSize.Height, referenceAreaPreferredSize.Height + importsAreaPreferredSize.Height + 6))
        End Function

        Protected Overrides Sub WndProc(ByRef m As Message)
            Try
                Dim processedDelayRefreshMessage As Boolean = False
                Select Case m.Msg
                    Case WmUserConstants.WM_REFPAGE_REFERENCES_REFRESH
                        ProcessDelayUpdateItems()
                        processedDelayRefreshMessage = True
                    Case WmUserConstants.WM_REFPAGE_IMPORTCHANGED
                        SetDirty(ImportList)
                        processedDelayRefreshMessage = True
                    Case WmUserConstants.WM_REFPAGE_IMPORTS_REFRESH
                        Try
                            PopulateImportsList(True)
                        Finally
                            _needRefreshImportList = False
                        End Try
                        processedDelayRefreshMessage = True
                    Case WmUserConstants.WM_REFPAGE_SERVICEREFERENCES_REFRESH
                        RefreshServiceReferences()
                        processedDelayRefreshMessage = True
                End Select

                If processedDelayRefreshMessage Then
                    Internal.Performance.CodeMarkers.Instance.CodeMarker(Internal.Performance.RoslynCodeMarkerEvent.PerfMSVSEditorsReferencePagePostponedUIRefreshDone)
                End If
            Catch ex As COMException
                ' The message pump in the background compiler could process our pending message, and when the compiler is running, we would get E_PENDING failure
                ' we want to post the message back to try it again.  To prevent spinning the main thread, we ask a background thread to post the message back after a short period of time
                If ex.ErrorCode = NativeMethods.E_PENDING Then
                    Dim delayMessage As New System.Threading.Timer(AddressOf DelayPostingMessage, m.Msg, 200, Timeout.Infinite)
                    Return
                End If
                Throw
            End Try

            MyBase.WndProc(m)
        End Sub

        ''' <summary>
        ''' We cannot process the UI refreshing message when compiler is running. However the compiler continually pumps messages.
        '''  It is a workaround to use background thread to wait for the compiler to finish.
        ''' Note: it rarely happens. (It happens we have a post message when a third party start the compiler and wait for something.)
        ''' </summary>
        Private Sub DelayPostingMessage(messageId As Object)
            If Not IsDisposed Then
                NativeMethods.PostMessage(Handle, CInt(messageId), 0, 0)
            End If
        End Sub

        ''' <summary>
        ''' Called when the page is activated or deactivated
        ''' </summary>
        ''' <param name="activated"></param>
        Protected Overrides Sub OnPageActivated(activated As Boolean)
            MyBase.OnPageActivated(activated)
            If IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub

        ''' <summary>
        ''' ;ReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a project reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="ref">Reference to take extract information from</param>
        ''' <param name="refObject">Internal Reference Object, which we push to the property grid</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList() and UnusedReferencePropPage</remarks>
        Friend Shared Function ReferenceToListViewItem(ref As VSLangProj.Reference, refObject As Object) As ListViewItem

            Debug.Assert(ref IsNot Nothing)

            Dim lvi As ListViewItem
            Dim CopyLocalText As String

            If ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeActiveX AndAlso ref.Description <> "" Then
                'For COM references with a nice description, use this
                '(like "Microsoft Office 10.0 Object Library" instead of "Office")
                lvi = New ListViewItem(ref.Description)
            Else
                lvi = New ListViewItem(ref.Name)
            End If

            lvi.Tag = refObject

            lvi.Checked = ref.CopyLocal
            CopyLocalText = ref.CopyLocal.ToString(CultureInfo.CurrentUICulture)

            If ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeActiveX Then
                lvi.SubItems.Add("COM")
            ElseIf ref.Type = VSLangProj.prjReferenceType.prjReferenceTypeAssembly Then
                lvi.SubItems.Add(".NET")
            Else
                lvi.SubItems.Add("UNKNOWN") 'Type
            End If
            lvi.SubItems.Add(ref.Version.ToString()) 'Version
            lvi.SubItems.Add(CopyLocalText) 'CopyLocal column

            ' We should put an error message there if we can not resolve the reference...
            Dim path As String = ref.Path
            If String.IsNullOrEmpty(path) Then
                path = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ReferenceNotFound
            End If

            lvi.SubItems.Add(path)

            Return lvi

        End Function

        ''' <summary>
        ''' WebReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a web reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="webref">WebReference project item</param>
        ''' <param name="refObject">Internal Reference Object</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList()</remarks>
        Private Shared Function WebReferenceToListViewItem(webRef As EnvDTE.ProjectItem, refObject As Object) As ListViewItem
            Debug.Assert(webRef IsNot Nothing)

            Dim lvi As ListViewItem

            lvi = New ListViewItem(webRef.Name) With {
                .Tag = refObject
            }

            lvi.SubItems.Add("WEB") 'Type
            lvi.SubItems.Add("") ' Version
            lvi.SubItems.Add("") ' Copy Local
            lvi.SubItems.Add(CStr(webRef.Properties.Item("WebReference").Value)) 'Path

            Return lvi
        End Function

        ''' <summary>
        ''' ServiceReferenceToListViewItem 
        ''' Creates a four column listview item from the information of a web reference.
        ''' These columns are: Reference Name, Type (COM/.NET/UNKNOWN), Version, Copy Local (Yes/No), Path
        ''' </summary>
        ''' <param name="serviceReference">service reference component</param>
        ''' <returns>ListViewItem object containing information from reference</returns>
        ''' <remarks>Helper for RefreshReferenceList()</remarks>
        Private Shared Function ServiceReferenceToListViewItem(serviceReference As ServiceReferenceComponent) As ListViewItem
            Debug.Assert(serviceReference IsNot Nothing)

            Dim lvi As ListViewItem

            lvi = New ListViewItem(serviceReference.[Namespace]) With {
                .Tag = serviceReference
            }

            lvi.SubItems.Add("SERVICE") 'Type
            lvi.SubItems.Add("") ' Version
            lvi.SubItems.Add("") ' Copy Local

            Dim referencePath As String
            Try
                referencePath = serviceReference.ServiceReferenceURL
            Catch ex As Exception
                ' show the error message, if the reference is broken
                referencePath = ex.Message
            End Try

            lvi.SubItems.Add(referencePath) 'Path

            Return lvi
        End Function

        ''' <summary>
        ''' Refreshes the reference listviews (both regular and web references), based on the list of references ReferenceListData.
        ''' </summary>
        ''' <param name="ReferenceListData">reference object lists</param>
        Private Sub RefreshReferenceList(ReferenceListData As ArrayList)

            ReferenceList.BeginUpdate()
            Try
                ReferenceList.View = View.Details
                ReferenceList.Items.Clear()

                'For Each ref As VSLangProj.Reference In theVSProject.References
                For refIndex As Integer = 0 To ReferenceListData.Count - 1
                    Dim refObject As Object = ReferenceListData(refIndex)
                    If TypeOf refObject Is ReferenceComponent Then
                        Debug.Assert(Not IsImplicitlyAddedReference(CType(refObject, ReferenceComponent).CodeReference), "Implicitly added references should have been filtered out and never displayed in our list")
                        ReferenceList.Items.Add(ReferenceToListViewItem(CType(refObject, ReferenceComponent).CodeReference, refObject))
                    ElseIf TypeOf refObject Is WebReferenceComponent Then
                        ReferenceList.Items.Add(WebReferenceToListViewItem(CType(refObject, WebReferenceComponent).WebReference, refObject))
                    ElseIf TypeOf refObject Is ServiceReferenceComponent Then
                        ReferenceList.Items.Add(ServiceReferenceToListViewItem(CType(refObject, ServiceReferenceComponent)))
                    End If
                Next

                ReferenceList.Sort()

            Finally
                ReferenceList.EndUpdate()
            End Try

            If Not _columnWidthUpdated Then
                SetReferenceListColumnWidths(Me, ReferenceList, 0)
                _columnWidthUpdated = True
            End If

            ReferenceList.Refresh()

            EnableReferenceGroup()

        End Sub

        ''' <summary>
        ''' Populates the Reference object of all references (regular and web) currently in the project, and also 
        '''   calls RefreshReferenceList() to update the listviews with those objects
        ''' </summary>
        Private Sub PopulateReferenceList()

            Dim theVSProject As VSLangProj.VSProject
            Dim ReferenceCount As Integer
            Dim ref As VSLangProj.Reference

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            ReferenceCount = theVSProject.References.Count

            HoldSelectionChange(True)
            Try
                Dim ReferenceListData As New ArrayList(ReferenceCount)

                For refIndex As Integer = 0 To ReferenceCount - 1
                    ref = theVSProject.References.Item(refIndex + 1) '1-based

                    'Don't worry about implicitly-added references (these can't be removed, and don't
                    '  show up in the solution explorer, so we don't want to show them in the property
                    '  pages, either - for VB, this is currently mscorlib and ms.vb.dll)
                    If Not IsImplicitlyAddedReference(ref) Then
                        ReferenceListData.Add(New ReferenceComponent(ref))
                    End If
                Next refIndex

                If theVSProject.WebReferencesFolder IsNot Nothing Then
                    For Each webRef As EnvDTE.ProjectItem In theVSProject.WebReferencesFolder.ProjectItems
                        ' we need check whether the project item is a web reference.
                        ' user could add random items under this folder
                        If IsWebReferenceItem(webRef) Then
                            ReferenceListData.Add(New WebReferenceComponent(Me, webRef))
                        End If
                    Next
                End If

                If _referenceGroupManager Is Nothing Then
                    Dim referenceManagerFactory As IVsWCFReferenceManagerFactory = CType(ServiceProvider.GetService(GetType(SVsWCFReferenceManagerFactory)), IVsWCFReferenceManagerFactory)
                    If referenceManagerFactory IsNot Nothing Then
                        Dim vsHierarchy As IVsHierarchy = ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject)
                        If vsHierarchy IsNot Nothing AndAlso IsServiceReferenceValidInProject(vsHierarchy) AndAlso referenceManagerFactory.IsReferenceManagerSupported(vsHierarchy) <> 0 Then
                            _referenceGroupManager = referenceManagerFactory.GetReferenceManager(vsHierarchy)
                        End If
                    End If
                End If

                If _referenceGroupManager IsNot Nothing Then
                    Dim collection As IVsWCFReferenceGroupCollection = _referenceGroupManager.GetReferenceGroupCollection()
                    For i As Integer = 0 To collection.Count() - 1
                        Dim referenceGroup As IVsWCFReferenceGroup = collection.Item(i)
                        ReferenceListData.Add(New ServiceReferenceComponent(collection, referenceGroup))
                    Next
                End If

                RefreshReferenceList(ReferenceListData)

                _delayUpdatingItems = Nothing
            Finally
                HoldSelectionChange(False)
            End Try

            PushSelection()
        End Sub

        ''' <summary>
        ''' check whether a project item is really a web reference
        ''' </summary>
        ''' <param name="webRef"></param>
        Private Shared Function IsWebReferenceItem(webRef As EnvDTE.ProjectItem) As Boolean
            Dim webRefProperty As EnvDTE.Property = Nothing
            Dim properties As EnvDTE.Properties = webRef.Properties
            If properties IsNot Nothing Then
                Try
                    webRefProperty = properties.Item("WebReferenceInterface")
                Catch ex As ArgumentException
                    ' Ignore those items which is actually not web reference (but random items added by user into the directory.)
                End Try
            End If
            Return webRefProperty IsNot Nothing
        End Function

        Public Function GetReferencedNamespaceList() As IList(Of String)
            Dim threadedWaitDialogFactory = DirectCast(ServiceProvider.GetService(GetType(SVsThreadedWaitDialogFactory)), IVsThreadedWaitDialogFactory)
            Dim threadedWaitDialog2 As IVsThreadedWaitDialog2 = Nothing
            ErrorHandler.ThrowOnFailure(threadedWaitDialogFactory.CreateInstance(threadedWaitDialog2))

            Dim threadedWaitDialog3 = DirectCast(threadedWaitDialog2, IVsThreadedWaitDialog3)
            Dim cancellationTokenSource As New CancellationTokenSource
            Dim cancellationCallback As New CancellationCallback(cancellationTokenSource)
            threadedWaitDialog3.StartWaitDialogWithCallback(
                My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ImportedNamespacesTitle,
                My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ComputingReferencedNamespacesMessage,
                szProgressText:=Nothing,
                varStatusBmpAnim:=Nothing,
                szStatusBarText:=Nothing,
                fIsCancelable:=True,
                iDelayToShowDialog:=1,
                fShowProgress:=True,
                iTotalSteps:=0,
                iCurrentStep:=0,
                pCallback:=cancellationCallback)

            Try
                Dim componentModel = DirectCast(ServiceProvider.GetService(GetType(SComponentModel)), IComponentModel)
                Dim visualStudioWorkspace = componentModel.GetService(Of VisualStudioWorkspace)
                Dim solution = visualStudioWorkspace.CurrentSolution

                For Each project In solution.Projects

                    ' We need to find the project that matches by project file path
                    If project.FilePath IsNot Nothing AndAlso String.Compare(project.FilePath, DTEProject.FullName, ignoreCase:=True) = 0 Then
                        Dim compilationTask = project.GetCompilationAsync(cancellationTokenSource.Token)
                        compilationTask.Wait(cancellationTokenSource.Token)
                        Dim compilation = compilationTask.Result

                        Dim namespaceNames As New List(Of String)
                        Dim namespacesToProcess As New Stack(Of INamespaceSymbol)
                        namespacesToProcess.Push(compilation.GlobalNamespace)

                        Do While namespacesToProcess.Count > 0
                            cancellationTokenSource.Token.ThrowIfCancellationRequested()

                            Dim namespaceToProcess = namespacesToProcess.Pop()

                            For Each childNamespace In namespaceToProcess.GetNamespaceMembers()
                                If NamespaceIsReferenceableFromCompilation(childNamespace, compilation) Then
                                    namespaceNames.Add(childNamespace.ToDisplayString())
                                End If

                                namespacesToProcess.Push(childNamespace)
                            Next
                        Loop

                        namespaceNames.Sort(CaseInsensitiveComparison.Comparer)
                        Return namespaceNames
                    End If
                Next

                ' Return empty list if an error occurred
                Return Array.Empty(Of String)
            Catch ex As OperationCanceledException
                ' Return empty list if we canceled
                Return Array.Empty(Of String)
            Finally
                Dim canceled As Integer = 0
                threadedWaitDialog3.EndWaitDialog(canceled)
            End Try
        End Function

        Private Class CancellationCallback
            Implements IVsThreadedWaitDialogCallback

            Private ReadOnly _cancellationTokenSource As CancellationTokenSource

            Public Sub New(cancellationTokenSource As CancellationTokenSource)
                _cancellationTokenSource = cancellationTokenSource
            End Sub

            Public Sub OnCanceled() Implements IVsThreadedWaitDialogCallback.OnCanceled
                _cancellationTokenSource.Cancel()
            End Sub
        End Class

        Private Shared Function NamespaceIsReferenceableFromCompilation([namespace] As INamespaceSymbol, compilation As Compilation) As Boolean
            For Each typeMember In [namespace].GetTypeMembers()
                If typeMember.CanBeReferencedByName Then
                    If typeMember.DeclaredAccessibility = CodeAnalysis.Accessibility.Public Then
                        Return True
                    End If

                    If SymbolEqualityComparer.Default.Equals(typeMember.ContainingAssembly, compilation.Assembly) OrElse typeMember.ContainingAssembly.GivesAccessTo(compilation.Assembly) Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        Private Sub PopulateImportsList(InitSelections As Boolean, Optional RemoveInvalidEntries As Boolean = False)
            Dim Namespaces As IList(Of String)
            Dim UserImports As String()

            If ServiceProvider Is Nothing Then
                'We may be tearing down...
                Return
            End If

            ' get namespace list earlier to prevent reentrance in this function to cause duplicated items in the list
            Namespaces = GetReferencedNamespaceList()
            UserImports = GetCurrentImports()

            ' Gotta make a copy of the currently selected items so I can re-select 'em after
            ' I have repopulated the list...
            Dim currentlySelectedItems As New Specialized.StringCollection
            For Each SelectedItem As String In ImportList.SelectedItems
                currentlySelectedItems.Add(SelectedItem)
            Next
            Dim TopIndex As Integer = ImportList.TopIndex

            'CurrentList is a dictionary whose keys are all the items which are
            '  currently in the imports listbox or are in the referenced namespaces 
            '  of the project or are imports added by the user.
            'The value of the entry is True if it is a reference namespace or user
            '  import.
            Dim CurrentListMap As New Dictionary(Of String, Boolean)

            'Initialize CurrentListMap to include keys from everything currently
            '  in the listbox.  We'll next mark as true only those that the compiler
            '  and project actually know about.
            For Each cItem As String In ImportList.Items
                CurrentListMap.Add(cItem, False)
            Next

            'Create a combined list of referenced namespaces and user-defined imports
            Dim NamespacesAndUserImports As New List(Of String)
            NamespacesAndUserImports.AddRange(Namespaces)
            NamespacesAndUserImports.AddRange(UserImports)

            'For each item of NamespacesAndUserImports, make sure the item is in the
            '   imports listbox, and also set its entry in CurrentListMap to True so
            '   we know it's a current namespace or user import
            For Each name As String In NamespacesAndUserImports
                If name.Length > 0 Then
                    If Not CurrentListMap.ContainsKey(name) Then
                        'Not already in the listbox - add it
                        ImportList.Items.Add(name)
                        CurrentListMap.Add(name, True)
                    Else
                        CurrentListMap.Item(name) = True
                    End If
                End If
            Next name

            If RemoveInvalidEntries Then
                For Each item As KeyValuePair(Of String, Boolean) In CurrentListMap
                    If item.Value = False Then
                        'The item is not in the referenced namespaces and it's not in the
                        '  user-defined imports list (i.e., the namespace no longer exists, or
                        '  it's a user-import that the user has previously unchecked)
                        ImportList.Items.Remove(item.Key)
                    End If
                Next
            End If

            If InitSelections Then
                CheckCurrentImports()
            End If

            For Each item As String In currentlySelectedItems
                Dim itemIndex As Integer = ImportList.Items.IndexOf(item)
                If itemIndex <> -1 Then
                    ImportList.SetSelected(itemIndex, True)
                End If
            Next

            If TopIndex < ImportList.Items.Count Then
                ImportList.TopIndex = TopIndex
            End If

            EnableImportGroup()
        End Sub

        Private Sub AddNamespaceToImportList(ns As String)
            If ImportList.Items.IndexOf(ns) = -1 Then
                ImportList.Items.Add(ns)
            End If
        End Sub

        Private Sub SelectNamespaceInImportList(_namespace As String, MoveToTop As Boolean)
            Dim index As Integer
            index = ImportList.Items.IndexOf(_namespace)
            If index = -1 AndAlso Not MoveToTop Then
                'We skip this step if MoveToTop is true so we avoid adding then moving 
                'This should only be able to occur if a namespace
                '  is not in the references
                AddNamespaceToImportList(_namespace)
                index = ImportList.Items.IndexOf(_namespace)
            End If
            Try
                _updatingImportList = True
                If MoveToTop Then
                    If index <> -1 Then
                        ImportList.Items.RemoveAt(index)
                    End If
                    ImportList.Items.Insert(0, _namespace)
                    ImportList.SetItemChecked(0, True)
                Else
                    ImportList.SetItemChecked(index, True)
                End If
            Finally
                _updatingImportList = False
            End Try
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

            PopulateReferenceList()
            PopulateImportsList(False)

            AdviseReferencesEvents(CType(DTEProject.Object, VSLangProj.VSProject))
            AdviseWebReferencesEvents()
            AdviseServiceReferencesEvents()
            AdviseImportsEvents(CType(DTEProject.Object, VSLangProj.VSProject))
        End Sub

        Private Function GetCurrentImports() As String()

            Dim threadedWaitDialogFactory = DirectCast(ServiceProvider.GetService(GetType(SVsThreadedWaitDialogFactory)), IVsThreadedWaitDialogFactory)
            Dim threadedWaitDialog2 As IVsThreadedWaitDialog2 = Nothing
            ErrorHandler.ThrowOnFailure(threadedWaitDialogFactory.CreateInstance(threadedWaitDialog2))

            Dim threadedWaitDialog3 = DirectCast(threadedWaitDialog2, IVsThreadedWaitDialog3)
            Dim cancellationTokenSource As New CancellationTokenSource
            Dim cancellationCallback As New CancellationCallback(cancellationTokenSource)
            threadedWaitDialog3.StartWaitDialogWithCallback(
                My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_CurrentImportsTitle,
                My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ComputingCurrentImportsMessage,
                szProgressText:=Nothing,
                varStatusBmpAnim:=Nothing,
                szStatusBarText:=Nothing,
                fIsCancelable:=True,
                iDelayToShowDialog:=1,
                fShowProgress:=True,
                iTotalSteps:=0,
                iCurrentStep:=0,
                pCallback:=cancellationCallback)

            Try
                Dim vsImports As VSLangProj.Imports = CType(DTEProject.Object, VSLangProj.VSProject).Imports
                Dim result As New List(Of String)(vsImports.Count)
                result.AddRange(vsImports.Cast(Of String)())
                result.Sort(CaseInsensitiveComparison.Comparer)
                Return result.ToArray()

            Catch ex As OperationCanceledException
                ' Return empty list if we canceled
                Return Array.Empty(Of String)()
            Finally
                Dim canceled As Integer = 0
                threadedWaitDialog3.EndWaitDialog(canceled)
            End Try
        End Function

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
            EnableReferenceGroup()
            EnableImportGroup()

            ' make the import-panel act as if it lost focus so that the selected-row color
            '   of the Imports CheckedListBox does not look like it is focused
            '
            ImportPanel_Leave(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Take a snapshot of the user defined imports
        ''' </summary>
        ''' <returns>A dictionary with import name/is namespace pairs</returns>
        Private Function GetUserDefinedImportsSnapshot() As IDictionary(Of String, Boolean)
            ' First, we get a collection of referenced namespaces that is fast to 
            ' search...
            Dim ReferencedNamespaces As New Hashtable
            For Each ReferencedNamespace As String In GetReferencedNamespaceList()
                If ReferencedNamespace <> "" Then
                    ReferencedNamespaces.Add(ReferencedNamespace, Nothing)
                End If
            Next
            ' We save all currently imported namespaces
            ' Each import is stored in the hashtable with the
            ' value set to "True" if it is a namespace known to the compiler
            Dim UserDefinedImports As New Dictionary(Of String, Boolean)
            For Each UserImport As String In GetCurrentImports()
                UserDefinedImports.Add(UserImport, ReferencedNamespaces.Contains(UserImport))
            Next
            Return UserDefinedImports
        End Function

        ''' <summary>
        ''' Remove any user imported namespaces that were known to the compilerat the time the ImportsSnapshot
        ''' was taken
        ''' </summary>
        ''' <param name="ImportsSnapshot">
        ''' A snapshot of the project imports taken sometime before... 
        ''' </param>
        Private Function TrimUserImports(ImportsSnapshot As IDictionary(Of String, Boolean)) As String()
            ' Let's give the compiler time to update the namespace list - it looks like we may
            ' have a race-condition here, but I can't find out why.... and o
            Thread.Sleep(10)

            ' First, we get a collection of referenced namespaces that is fast to 
            ' search...
            Dim ReferencedNamespaces As New Hashtable
            For Each ReferencedNamespace As String In GetReferencedNamespaceList()
                If ReferencedNamespace <> "" Then
                    ReferencedNamespaces.Add(ReferencedNamespace, Nothing)
                End If
            Next

            Dim ResultList As New List(Of String)
            Dim snapshot As IEnumerable(Of KeyValuePair(Of String, Boolean)) = ImportsSnapshot
            For Each PreviousImportEntry As KeyValuePair(Of String, Boolean) In snapshot
                If PreviousImportEntry.Value Then
                    ' This was a namespace known to the compiler before whatever references were removed...
                    ' Only add it to the result if it is still known!
                    If ReferencedNamespaces.Contains(PreviousImportEntry.Key) Then
                        ResultList.Add(PreviousImportEntry.Key)
                    End If
                Else
                    ResultList.Add(PreviousImportEntry.Key)
                End If
            Next
            Return ResultList.ToArray()
        End Function

        Private Sub RemoveReference_Click(sender As Object, e As EventArgs) Handles RemoveReference.Click
            RemoveSelectedReference()
        End Sub

        Private Sub RemoveSelectedReference()
            Dim ItemIndices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices
            Dim ItemIndex As Integer
            Dim ref As ReferenceComponent
            Dim refComponent As IReferenceComponent
            Dim ReferenceRemoved As Boolean = False 'True if one or more references was actually removed

            If ItemIndices.Count = 0 Then
                Return
            End If

            Using New WaitCursor
                Dim ImportsSnapshot As IDictionary(Of String, Boolean) = Nothing

                Using New ProjectBatchEdit(ProjectHierarchy)
                    Try
                        Dim errorString As String = Nothing
                        Dim refName As String = String.Empty

                        _updatingReferences = True
                        ReferenceList.BeginUpdate()

                        For i As Integer = ItemIndices.Count - 1 To 0 Step -1
                            Dim err As String = Nothing
                            ItemIndex = ItemIndices(i)

                            If ImportsSnapshot Is Nothing Then
                                ' Since we are going to remove a reference, and we haven't taken a snapshot of
                                ' the user imports before, we better do it now!
                                ImportsSnapshot = GetUserDefinedImportsSnapshot()
                            End If
                            'Remove from project references

                            EnterProjectCheckoutSection()
                            Try
                                refComponent = TryCast(ReferenceList.Items(ItemIndex).Tag, IReferenceComponent)
                                If refComponent IsNot Nothing Then
                                    ref = TryCast(refComponent, ReferenceComponent)
                                    If ref IsNot Nothing Then
                                        If IsImplicitlyAddedReference(ref.CodeReference) Then
                                            Debug.Fail("Implicitly added references should have been filtered out and never displayed in our list")
                                            Continue For
                                        End If
                                    End If

                                    refName = refComponent.GetName()
                                    refComponent.Remove()
                                    ReferenceRemoved = True
                                Else
                                    Debug.Fail("Unknown reference item")
                                End If

                                'Remove from local storage
                                ReferenceList.Items.RemoveAt(ItemIndex)

                            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(RemoveSelectedReference), NameOf(ReferencePropPage))
                                If ProjectReloadedDuringCheckout Then
                                    ' If the Project could be reloaded, we should return ASAP, because the designer has been disposed
                                    Return
                                End If

                                If IsCheckoutCanceledException(ex) Then
                                    'User already saw a message, no need to show an error message.  Also, don't
                                    '  want to continue trying to remove references.
                                    Exit For
                                Else
                                    ' some reference can not be removed (like mscorlib)
                                    err = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_CanNotRemoveReference, refName, ex.Message)
                                End If
                            Finally
                                LeaveProjectCheckoutSection()
                            End Try

                            If err IsNot Nothing Then
                                If errorString Is Nothing Then
                                    errorString = err
                                Else
                                    errorString += err
                                End If
                            End If
                        Next

                        If errorString IsNot Nothing Then
                            ShowErrorMessage(errorString)
                        End If

                    Finally
                        ' If the Project is reloaded, don't do anything as the page is disposed. VSWhidbey: 595444
                        If Not ProjectReloadedDuringCheckout Then
                            ReferenceList.EndUpdate()

                            ' Update buttons...
                            EnableReferenceGroup()
                            EnableImportGroup()
                            _updatingReferences = False
                        End If
                    End Try
                End Using

                If ReferenceRemoved Then
                    ' Now, we better remove any user imports that is no longer 
                    ' known to the compiler...
                    If ImportsSnapshot IsNot Nothing Then
                        SaveImportedNamespaces(TrimUserImports(ImportsSnapshot))
                    End If

                    'RemoveInvalidEntries=True here because so that we can remove imports
                    '  that correspond to the removed references, instead of just unchecking
                    '  them.  This will also clean up any other invalid unchecked imports in 
                    '  the list, which might be a minor surprise to the user, but shouldn't be
                    '  too bad, and is the safest fix at this point in the schedule.
                    PopulateImportsList(InitSelections:=True, RemoveInvalidEntries:=True)
                    SetDirty(ImportList)
                End If
            End Using

        End Sub

        Private Sub addContextMenuStrip_Opening(sender As Object, e As CancelEventArgs) Handles addContextMenuStrip.Opening
            Dim vsHierarchy As IVsHierarchy = ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject)
            If vsHierarchy IsNot Nothing Then
                webReferenceToolStripMenuItem.Visible = IsWebReferenceSupportedByDefaultInProject(vsHierarchy)
                serviceReferenceToolStripMenuItem.Visible = IsServiceReferenceValidInProject(vsHierarchy)
            End If
        End Sub

        Private Sub referenceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles referenceToolStripMenuItem.Click, addSplitButton.Click
            Dim UIHier = TryCast(ProjectHierarchy, IVsUIHierarchy)
            If UIHier IsNot Nothing Then
                Try
                    Const ECMD_ADDREFERENCE As Integer = 1113

                    Dim CmdCount As UInteger = 1
                    Dim cmd As OLE.Interop.OLECMD() = New OLE.Interop.OLECMD(0) {}
                    Dim hr As Integer

                    cmd(0).cmdID = ECMD_ADDREFERENCE
                    cmd(0).cmdf = 0
                    Dim guidVSStd2k As New Guid(&H1496A755, &H94DE, &H11D0, &H8C, &H3F, &H0, &HC0, &H4F, &HC2, &HAA, &HE2)

                    VSErrorHandler.ThrowOnFailure(UIHier.QueryStatusCommand(VSITEMID.ROOT, guidVSStd2k, CmdCount, cmd, Nothing))

                    'Adding a reference requires a project file checkout.  Do this now to avoid nasty checkout issues.

                    Dim ProjectReloaded As Boolean = False
                    CheckoutProjectFile(ProjectReloaded)
                    If ProjectReloaded Then
                        Return
                    End If

                    hr = UIHier.ExecCommand(VSITEMID.ROOT, guidVSStd2k, ECMD_ADDREFERENCE, 0, Nothing, IntPtr.Zero)
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(referenceToolStripMenuItem_Click), NameOf(ReferencePropPage))
                    ShowErrorMessage(ex)
                End Try

                'Refresh the references
                ProcessDelayUpdateItems()
            End If
        End Sub

        Private Sub webReferenceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles webReferenceToolStripMenuItem.Click
            Dim AddWebRefDlg As IVsAddWebReferenceDlg2
            Dim DiscoveryResult As IDiscoveryResult = Nothing

            AddWebRefDlg = CType(ServiceProvider.GetService(GetType(IVsAddWebReferenceDlg)), IVsAddWebReferenceDlg2)

            Dim Cancelled As Integer
            Dim url As String = Nothing
            Dim newName As String = Nothing

            Try
                'Adding a reference requires a project file checkout.  Do this now to avoid nasty checkout issues.
                Dim ProjectReloaded As Boolean = False
                CheckoutProjectFile(ProjectReloaded)
                If ProjectReloaded Then
                    Return
                End If

                VSErrorHandler.ThrowOnFailure(AddWebRefDlg.AddWebReferenceDlg(Nothing, url, newName, DiscoveryResult, Cancelled))
                If Cancelled = 0 Then
                    'CONSIDER: Shouldn't this be cached and applied by 'Apply' button?
                    Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                    Dim item As EnvDTE.ProjectItem = theVSProject.AddWebReference(url)
                    If Not String.Equals(item.Name, newName, StringComparison.Ordinal) Then
                        item.Name = newName
                    End If
                    PopulateReferenceList()
                    PopulateImportsList(True)
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(webReferenceToolStripMenuItem_Click), NameOf(ReferencePropPage))
                If Not IsCheckoutCanceledException(ex) Then
                    ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_AddWebReference, ex.Message))
                End If
            End Try
        End Sub

        Private Sub serviceReferenceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles serviceReferenceToolStripMenuItem.Click
            Dim AddServiceRefDlg As IVsAddWebReferenceDlg3

            AddServiceRefDlg = CType(ServiceProvider.GetService(GetType(SVsAddWebReferenceDlg3)), IVsAddWebReferenceDlg3)

            Debug.Assert(AddServiceRefDlg IsNot Nothing, "Why we couldn't find ASR dialog service")
            If AddServiceRefDlg IsNot Nothing Then
                Dim result As IVsAddWebReferenceResult = Nothing
                Dim Cancelled As Integer = 0
                Dim serviceType As ServiceReferenceType = ServiceReferenceType.SRT_WCFReference Or ServiceReferenceType.SRT_ASMXReference

                Try
                    AddServiceRefDlg.ShowAddWebReferenceDialog(
                                ShellUtil.VsHierarchyFromDTEProject(ServiceProvider, DTEProject),
                                Nothing,
                                serviceType,
                                Nothing,
                                Nothing,
                                Nothing,
                                result,
                                Cancelled)
                    If Cancelled = 0 Then
                        Debug.Assert(Not _updatingReferences, "We shouldn't be in another updating session")

                        _updatingReferences = True
                        Try
                            result.Save()
                        Finally
                            _updatingReferences = False
                        End Try

                        PopulateReferenceList()
                        PopulateImportsList(True)

                        Internal.Performance.CodeMarkers.Instance.CodeMarker(Internal.Performance.RoslynCodeMarkerEvent.PerfMSVSEditorsReferencePageWCFAdded)
                    End If
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(serviceReferenceToolStripMenuItem_Click), NameOf(ReferencePropPage))
                    If Not IsCheckoutCanceledException(ex) Then
                        ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_AddWebReference, ex.Message))
                    End If
                End Try
            End If

        End Sub

        Private Sub EnableReferenceGroup()
            Dim items As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems

            Dim removeReferencesButtonEnabled As Boolean = items.Count > 0

            ' if the remove-reference button is enabled AND if it is going to be disabled AND if
            '   the button contains focus, then the logical place to put focus is on the ReferenceList
            '   ListView so the user can continue to interact with references
            '
            If RemoveReference.Enabled AndAlso Not removeReferencesButtonEnabled AndAlso RemoveReference.ContainsFocus Then
                ActiveControl = ReferenceList
            End If

            'Enable/Disable Remove button
            RemoveReference.Enabled = removeReferencesButtonEnabled

            'Enable/Disable Update button (valid for Web references only)
            For i As Integer = 0 To items.Count - 1
                If TryCast(items(i).Tag, IUpdatableReferenceComponent) Is Nothing Then
                    UpdateReferences.Enabled = False
                    Return
                End If
            Next

            UpdateReferences.Enabled = items.Count > 0
        End Sub

        Private Sub EnableImportGroup()
            Dim EnableAddImportButton As Boolean = False
            Dim EnableUpdateUserImportButton As Boolean = False
            Dim ScrubbedUserImportText As String = UserImportTextBox.Text.Trim()

            If ScrubbedUserImportText <> "" Then
                ' Check if the item already exists in the list box - if so, don't allow users to
                ' add/update the item
                ' We can't use the listbox.items.contains method, since that would be a case-sensitive
                ' lookup, and we don't want that!
                Dim itemAlreadyExists As Boolean = False

                Dim userImportId As New ImportIdentity(ScrubbedUserImportText)
                For Each KnownItem As String In ImportList.Items
                    If userImportId.Equals(New ImportIdentity(KnownItem)) Then
                        itemAlreadyExists = True
                        Exit For
                    End If
                Next

                If Not itemAlreadyExists Then
                    ' The "Add user imports" button should be enabled iff:
                    ' * The text in the add user import textbox isn't empty AND
                    ' * The import list box doesn't already contain this item
                    EnableAddImportButton = True

                    ' The "Update user import" button should be enabled iff:
                    ' * There is only one item selected in the imports list box
                    ' * The list of known namespaces retrieved from the compiler doesn't 
                    '   contain the currently selected item in the imports list box
                    '   (we can't modify those imports...)
                    Debug.Assert(ImportListSelectedItem IsNot Nothing, "ImportListSelectedItem should not return Nothing")
                    If (ImportListSelectedItem IsNot Nothing) AndAlso (ImportListSelectedItem.Length > 0) Then
                        EnableUpdateUserImportButton = True
                        Dim selectedItemIdentity As New ImportIdentity(DirectCast(ImportListSelectedItem, String))
                        For Each NamespaceKnownByTheCompiler As String In GetReferencedNamespaceList()
                            If selectedItemIdentity.Equals(New ImportIdentity(NamespaceKnownByTheCompiler)) Then
                                EnableUpdateUserImportButton = False
                                Exit For
                            End If
                        Next
                    End If
                Else
                    'The item's key is already in the list (for aliased imports, this means that the alias is in the
                    '  list).  There's one other case where we want to enable the Update User Import button - the
                    '  case where they want to change an aliased or XML import.  I.e., suppose they have "a=MS.VB" in 
                    '  the list and they want to change it to "a=MS.VB.Compatibility".  In this case, itemAlreadyExists
                    '  is true because the key "a" is already in the list.  So, if the key of the selected item
                    '  is the same as the key of the item in the textbox, and the full text of the two is not the
                    '  same, then we enable the Update User Import button.
                    Debug.Assert(ImportListSelectedItem IsNot Nothing, "ImportListSelectedItem should not return Nothing")
                    If (ImportListSelectedItem IsNot Nothing) AndAlso (ImportListSelectedItem.Length > 0) Then
                        Dim selectedItemIdentity As New ImportIdentity(DirectCast(ImportListSelectedItem, String))
                        If userImportId.Equals(selectedItemIdentity) _
                                    AndAlso Not ScrubbedUserImportText.Equals(ImportListSelectedItem,
                                                                                StringComparison.Ordinal) Then
                            EnableUpdateUserImportButton = True
                        End If
                    End If
                End If
            End If

            AddUserImportButton.Enabled = EnableAddImportButton

            ' if the update-user-import button is enabled AND if it is going to be disabled AND if
            '   the button contains focus, then the logical place to put focus is on the ImportList
            '   CheckedListBox so the user can continue to interact with imports
            '
            If UpdateUserImportButton.Enabled AndAlso Not EnableUpdateUserImportButton AndAlso UpdateUserImportButton.ContainsFocus Then
                ActiveControl = ImportList
            End If
            UpdateUserImportButton.Enabled = EnableUpdateUserImportButton
        End Sub

        Private Sub ReferenceList_ItemActivate(sender As Object, e As EventArgs) Handles ReferenceList.ItemActivate
            Dim items As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems
            If items.Count > 0 Then
                DTE.ExecuteCommand("View.PropertiesWindow", String.Empty)
            End If
        End Sub

        Private Sub ReferenceList_KeyDown(sender As Object, e As KeyEventArgs) Handles ReferenceList.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim items As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems
                If items.Count > 0 Then
                    RemoveSelectedReference()
                End If
            End If
        End Sub

        Private Sub ReferenceList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ReferenceList.SelectedIndexChanged
            EnableReferenceGroup()

            PushSelection()
        End Sub

        Private Sub ReferenceList_Enter(sender As Object, e As EventArgs) Handles ReferenceList.Enter
            PushSelection()
        End Sub

        ''' <summary>
        '''  When the customer clicks a column header, we should sort the reference list
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ReferenceList_ColumnClick(sender As Object, e As ColumnClickEventArgs) Handles ReferenceList.ColumnClick
            ListViewComparer.HandleColumnClick(ReferenceList, _referenceSorter, e)
        End Sub

        Private Sub UpdateReferences_Click(sender As Object, e As EventArgs) Handles UpdateReferences.Click
            Using New WaitCursor
                Dim items As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems
                For Each item As ListViewItem In items
                    Dim referenceComponent As IUpdatableReferenceComponent = TryCast(item.Tag, IUpdatableReferenceComponent)
                    If referenceComponent IsNot Nothing Then
                        Try
                            referenceComponent.Update()
                        Catch ex As Exception When ReportWithoutCrash(ex, NameOf(UpdateReferences_Click), NameOf(ReferencePropPage))
                            If Not IsCheckoutCanceledException(ex) Then
                                ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_FailedToUpdateWebReference, CType(referenceComponent, IReferenceComponent).GetName(), ex.Message))
                            End If
                        End Try
                    End If
                Next
            End Using
        End Sub

        Private Sub UnusedReferences_Click(sender As Object, e As EventArgs) Handles UnusedReferences.Click
            ' Take a snapshot of the user imports...
            Dim ImportsSnapshot As IDictionary(Of String, Boolean) = GetUserDefinedImportsSnapshot()

            If ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceTitle, GetType(UnusedReferencePropPage)) = DialogResult.OK Then
                If SaveImportedNamespaces(TrimUserImports(ImportsSnapshot)) Then
                    'RemoveInvalidEntries=True here because so that we can remove imports
                    '  that correspond to the removed references, instead of just unchecking
                    '  them.  This will also clean up any other invalid unchecked imports in 
                    '  the list, which might be a minor surprise to the user, but shouldn't be
                    '  too bad, and is the safest fix at this point in the schedule.
                    PopulateImportsList(InitSelections:=True, RemoveInvalidEntries:=True)
                    SetDirty(ImportList)
                End If
            End If
        End Sub

        Private Sub ReferencePathsButton_Click(sender As Object, e As EventArgs) Handles ReferencePathsButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ReferencePaths_Title, GetType(ReferencePathsPropPage))
        End Sub

        Private Sub ImportList_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles ImportList.ItemCheck
            'Don't apply yet, this event is fired before the actual value has been updated
            If Not _updatingImportList Then
                NativeMethods.PostMessage(Handle, WmUserConstants.WM_REFPAGE_IMPORTCHANGED, e.Index, 0)
            End If
        End Sub

        ''' <summary>
        ''' Delegate for calling into RestoreImportListSelection.  Used by ImportPanel_Enter.
        ''' </summary>
        Private Delegate Sub RestoreImportListSelectionDelegate()

        ''' <summary>
        ''' In order to see the blue selection color, we need to restore the selection of the CheckedListBox when
        ''' focus comes into the control. When focus leaves the control, we remove the selection so that the
        ''' blue selection color is not shown [when it shows, it's visually confusing as to whether or not the control
        ''' still has focus].
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ImportPanel_Enter(sender As Object, e As EventArgs) Handles addUserImportTableLayoutPanel.Enter
            ' We restore the selection through a message pump. 
            ' The reason is vswhidbey 496909.
            ' When the user clicks an item in the ListBox to select it, we will get OnEnter first, then we noticed the selection change.
            ' We call BeginInvoke here, because it will go through the message loop to make sure we have a chance to handle the selection change event.
            BeginInvoke(New RestoreImportListSelectionDelegate(AddressOf RestoreImportListSelection))
        End Sub

        ''' <summary>
        ''' RestoreImportListSelection is called, when focus comes back into the ImportList area. We restore the selection.
        ''' However, if ImportList has already got a selection. We will know the user actually clicks (mouse) one item of the list.
        ''' In that case, we shouldn't restore the old selection (wswhibey 496909)
        ''' </summary>
        Private Sub RestoreImportListSelection()
            _hidingImportListSelectedItem = True
            Try
                If _importListSelectedItem IsNot Nothing Then
                    If ImportList.SelectedItem Is Nothing Then
                        ImportList.SelectedItem = _importListSelectedItem
                    End If
                    _importListSelectedItem = Nothing
                End If
            Finally
                _hidingImportListSelectedItem = False
            End Try
        End Sub

        ''' <summary>
        ''' In order to hide the blue selection color, we need to cache and remove the selection of the CheckedListBox when
        ''' focus leaves the control. When focus comes back into the control, we restore the selection so that the
        ''' blue selection color is shown.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ImportPanel_Leave(sender As Object, e As EventArgs) Handles addUserImportTableLayoutPanel.Leave
            _hidingImportListSelectedItem = True
            Try
                _importListSelectedItem = DirectCast(ImportList.SelectedItem, String)
                ImportList.SelectedItem = Nothing
            Finally
                _hidingImportListSelectedItem = False
            End Try
        End Sub

        '''<summary>
        ''' Imported namespaces are currently added/removed one at a time
        '''    All removes are processed first, and then adds 
        '''</summary>
        ''' <param name="newImportList">the imported list being saved</param>
        ''' <return>return true if any value was changed...</return>
        ''' <remarks>
        ''' CONSIDER: This is how the msvbprj code did it, and it may not work well
        '''         for other compilers (assuming this page is later shared)
        '''</remarks>
        Private Function SaveImportedNamespaces(newImportList As String()) As Boolean
            Dim valueUpdated As Boolean = False
            Dim vsImports As VSLangProj.Imports = CType(DTEProject.Object, VSLangProj.VSProject).Imports

            Debug.Assert(Not _ignoreImportEvent, "why m_ignoreImportEvent = TRUE?")
            Try
                _ignoreImportEvent = True

                'For backward compatibility we remove all non-imported ones from the current Imports before adding any new ones
                Dim currentImports = New List(Of String)(vsImports.Count)

                For index = vsImports.Count To 1 Step -1
                    Dim namespaceName As String = String.Empty
                    Try
                        namespaceName = vsImports.Item(index)
                        currentImports.Add(namespaceName)
                        If Not newImportList.Contains(namespaceName) Then
                            Debug.WriteLine("Removing reference: " & namespaceName)
                            vsImports.Remove(index)
                            valueUpdated = True
                        End If
                    Catch ex As Exception When ReportWithoutCrash(ex, "Unexpected error when removing imports", NameOf(ReferencePropPage))
                        If IsCheckoutCanceledException(ex) Then
                            'Exit early - no need to show any UI, they've already seen it
                            Return valueUpdated
                        ElseIf TypeOf ex Is COMException Then
                            ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_RemoveImportsFailUnexpected, namespaceName, Hex(DirectCast(ex, COMException).ErrorCode)))
                            Debug.Fail("Unexpected error when removing imports")
                        Else
                            ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_RemoveImportsFailUnexpected, namespaceName, ex.Message))
                            Debug.Fail("Unexpected error when removing imports")
                        End If
                    End Try
                Next index

                'Now add anything new
                For Each namespaceName As String In newImportList
                    Try
                        'Add it if not already in the list
                        If Not currentImports.Contains(namespaceName) Then
                            Debug.WriteLine("Adding reference: " & namespaceName)
                            vsImports.Add(namespaceName)
                            valueUpdated = True
                        End If
                    Catch ex As Exception When ReportWithoutCrash(ex, "Unexpected error when removing imports", NameOf(ReferencePropPage))
                        If IsCheckoutCanceledException(ex) Then
                            'Exit early - no need to show any UI, they've already seen it
                            Return valueUpdated
                        ElseIf TypeOf ex Is COMException Then
                            ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_RemoveImportsFailUnexpected, namespaceName, Hex(DirectCast(ex, COMException).ErrorCode)))
                            Debug.Fail("Unexpected error when removing imports")
                        Else
                            ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Reference_RemoveImportsFailUnexpected, namespaceName, ex.Message))
                            Debug.Fail("Unexpected error when removing imports")
                        End If
                    End Try
                Next
            Finally
                _ignoreImportEvent = False
            End Try
            Return valueUpdated
        End Function

        'Get the user selected values and update the project's Imports list
        Private Function ImportListGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            'Now add anything new
            Dim CheckedItems As CheckedListBox.CheckedItemCollection = ImportList.CheckedItems

            Dim List As String() = New String(CheckedItems.Count - 1) {}

            For ListIndex As Integer = 0 To List.Length - 1
                List(ListIndex) = DirectCast(CheckedItems.Item(ListIndex), String)
            Next

            value = List
            'Return True so base class sets the property
            Return True
        End Function

        Private Sub CheckCurrentImports(UserImports As String(), updateSelection As Boolean)
            'Check the user imports and sort them
            Dim SaveState As Boolean = m_fInsideInit
            Dim lastUpdatedNamespace As String = Nothing

            m_fInsideInit = True
            Try
                'Uncheck previously checked
                Dim selectedItem As New Specialized.StringCollection
                Try
                    _updatingImportList = True
                    For Each Index As Integer In ImportList.CheckedIndices
                        If Array.IndexOf(UserImports, ImportList.Items(Index)) < 0 Then
                            ImportList.SetItemChecked(Index, False)
                            lastUpdatedNamespace = CStr(ImportList.Items(Index))
                        Else
                            selectedItem.Add(CStr(ImportList.Items(Index)))
                        End If
                    Next
                Finally
                    _updatingImportList = False
                End Try

                Dim needAdjustOrder As Boolean = Not ContainsFocus

                'Now check ones we need to
                For UserIndex As Integer = UBound(UserImports) To 0 Step -1
                    If Not selectedItem.Contains(UserImports(UserIndex)) Then
                        SelectNamespaceInImportList(UserImports(UserIndex), needAdjustOrder)
                        lastUpdatedNamespace = UserImports(UserIndex)
                    End If
                Next

                If updateSelection AndAlso lastUpdatedNamespace IsNot Nothing Then
                    Dim lastChangedIndex As Integer = ImportList.Items.IndexOf(lastUpdatedNamespace)
                    If lastChangedIndex >= 0 Then
                        ImportList.TopIndex = lastChangedIndex
                        ImportList.SelectedIndex = lastChangedIndex
                        EnableImportGroup()
                    End If
                End If
            Finally
                m_fInsideInit = SaveState
            End Try
        End Sub

        Private Sub CheckCurrentImports()
            CheckCurrentImports(GetCurrentImports(), False)
        End Sub

        Private Function ImportListSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim UserImports As String() = DirectCast(value, String()) 'GetCurrentImportsList()
            'ControlData()(0).InitialValue = UserImports 'Save import list values
            CheckCurrentImports(UserImports, True)
            Return True
        End Function

        Protected Overrides Sub OnApplyComplete(ApplySuccessful As Boolean)
            'Refresh the lists
            m_fInsideInit = True
            Try
                PopulateReferenceList()
                PopulateImportsList(True)
            Finally
                m_fInsideInit = False
            End Try
        End Sub

        ''' <summary>
        ''' ;SetReferenceListColumnWidths
        ''' The Listview class does not support individual column widths, so we do it via sendmessage.
        ''' The ColOffset is used to support both ReferencePropPage and UnusedReferencePropPage, which have list
        ''' views with columns that are offset.
        ''' </summary>
        ''' <param name="owner">Control which owns the listview</param>
        ''' <param name="ReferenceList">The listview control to set column widths</param>
        ''' <param name="ColOffset">Offset to "Reference Name" column</param>
        Friend Shared Sub SetReferenceListColumnWidths(ByRef owner As Control, ByRef ReferenceList As ListView, ColOffset As Integer)
            Dim _handle As IntPtr = ReferenceList.Handle

            ' By default size all columns by size of column header text
            Dim AutoSizeMethod As Integer() = New Integer(REFCOLUMN_MAX) {NativeMethods.LVSCW_AUTOSIZE_USEHEADER, NativeMethods.LVSCW_AUTOSIZE_USEHEADER, NativeMethods.LVSCW_AUTOSIZE_USEHEADER, NativeMethods.LVSCW_AUTOSIZE_USEHEADER, NativeMethods.LVSCW_AUTOSIZE_USEHEADER}

            If ReferenceList.Items.Count > 0 Then
                ' If there are elements in the listview, size the name, version, and path columns by item text if not empty
                With ReferenceList.Items(0)
                    ' For the first column, if not offset, check the .text property, otherwise check the subitems
                    If (ColOffset = 0 AndAlso .Text <> "") OrElse
                        (ColOffset > 0 AndAlso .SubItems(REFCOLUMN_NAME + ColOffset).Text <> "") Then
                        AutoSizeMethod(REFCOLUMN_NAME) = NativeMethods.LVSCW_AUTOSIZE
                    End If

                    If .SubItems.Count > REFCOLUMN_VERSION + ColOffset AndAlso .SubItems(REFCOLUMN_VERSION + ColOffset).Text <> "" Then
                        AutoSizeMethod(REFCOLUMN_VERSION) = NativeMethods.LVSCW_AUTOSIZE
                    End If

                    If .SubItems.Count > REFCOLUMN_PATH + ColOffset AndAlso .SubItems(REFCOLUMN_PATH + ColOffset).Text <> "" Then
                        AutoSizeMethod(REFCOLUMN_PATH) = NativeMethods.LVSCW_AUTOSIZE
                    End If
                End With
            End If

            ' Do actual sizing
            NativeMethods.SendMessage(New HandleRef(owner, _handle), NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_NAME + ColOffset, AutoSizeMethod(REFCOLUMN_NAME))
            NativeMethods.SendMessage(New HandleRef(owner, _handle), NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_TYPE + ColOffset, AutoSizeMethod(REFCOLUMN_TYPE))
            NativeMethods.SendMessage(New HandleRef(owner, _handle), NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_VERSION + ColOffset, AutoSizeMethod(REFCOLUMN_VERSION))
            NativeMethods.SendMessage(New HandleRef(owner, _handle), NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_COPYLOCAL + ColOffset, AutoSizeMethod(REFCOLUMN_COPYLOCAL))
            NativeMethods.SendMessage(New HandleRef(owner, _handle), NativeMethods.LVM_SETCOLUMNWIDTH, REFCOLUMN_PATH + ColOffset, AutoSizeMethod(REFCOLUMN_PATH))
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            If ImportList.Focused Then
                Return HelpKeywords.VBProjPropImports
            End If
            Return HelpKeywords.VBProjPropReference
        End Function

        Private Sub ImportList_Validated(sender As Object, e As EventArgs) Handles ImportList.Validated
        End Sub

        ''' <summary>
        ''' Add the text in the user import text box as a new project level import.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub AddUserImportButton_Click(sender As Object, e As EventArgs) Handles AddUserImportButton.Click
            Debug.Assert(UserImportTextBox.Text.Trim().Length > 0, "Why was the AddUserImportButton enabled when the UserImport text was empty?")
            ' Get the current list
            Dim CurrentImports As String() = GetCurrentImports()
            Dim ScrubbedUserImport As String = UserImportTextBox.Text.Trim()

            'Make place for one more item...
            ReDim Preserve CurrentImports(CurrentImports.Length)

            '...add the new item...
            CurrentImports(CurrentImports.Length - 1) = ScrubbedUserImport

            '...and store it!
            If SaveImportedNamespaces(CurrentImports) Then
                'Add the item to the top of the listbox before updating the list, or else
                '  it will end up at the bottom.
                If ImportList.Items.IndexOf(ScrubbedUserImport) < 0 Then
                    ImportList.Items.Insert(0, ScrubbedUserImport)
                    ImportList.SelectedIndex = 0
                Else
                    Debug.Fail("The new item shouldn't have already been in the listbox")
                End If

                PopulateImportsList(True)
                SetDirty(ImportList)

                ' Let's make sure the new item is visible & selected!
                Dim newIndex As Integer = ImportList.Items.IndexOf(ScrubbedUserImport)
                ImportList.TopIndex = newIndex
                ImportList.SelectedIndex = newIndex
                EnableImportGroup()
            End If
        End Sub

        ''' <summary>
        ''' Update imports button / fill in imports text box with appropriate info every time the selected index
        ''' changes
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ImportList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ImportList.SelectedIndexChanged

            If Not _hidingImportListSelectedItem Then
                _importListSelectedItem = Nothing
                UserImportTextBox.Text = ImportListSelectedItem
                EnableImportGroup()
            End If

        End Sub

        ''' <summary>
        ''' Update the currently selected project level import
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub UpdateUserImportButton_Click(sender As Object, e As EventArgs) Handles UpdateUserImportButton.Click

            Debug.Assert(ImportList.SelectedItems.Count <= 1 AndAlso
                        ImportListSelectedItem IsNot Nothing AndAlso
                        ImportListSelectedItem.Length > 0, "Why do we try to update more than one selected item?!")

            Dim UserImports As String() = GetCurrentImports()
            Dim UserImportToUpdate As String = ImportListSelectedItem
            Dim ScrubbedUpdatedUserImport As String = UserImportTextBox.Text.Trim()

            Debug.Assert(UserImportToUpdate IsNot Nothing, "ImportListSelectedItem should not return Nothing")
            If UserImportToUpdate Is Nothing Then
                UserImportToUpdate = String.Empty
            End If

            Dim UserImportUpdated As Boolean = False
            For pos As Integer = 0 To UserImports.Length - 1
                If UserImports(pos).Equals(UserImportToUpdate, StringComparison.OrdinalIgnoreCase) Then
                    UserImports(pos) = ScrubbedUpdatedUserImport
                    UserImportUpdated = True
                    Exit For
                End If
            Next

            If UserImportUpdated AndAlso SaveImportedNamespaces(UserImports) Then
                'Modify the value in-place in the listbox
                Dim currentIndex As Integer = ImportList.Items.IndexOf(UserImportToUpdate)
                If currentIndex >= 0 Then
                    ImportList.Items(currentIndex) = ScrubbedUpdatedUserImport
                Else
                    Debug.Fail("Why didn't we find the old item?")
                End If

                PopulateImportsList(True)
                SetDirty(ImportList)
            End If

            ' Let's make sure the updated item is still selected...
            ' The PopulateImportsList failed to reset it, 'cause updating an import is really a remove/add operation
            Dim updatedItemIndex As Integer = ImportList.Items.IndexOf(ScrubbedUpdatedUserImport)
            If updatedItemIndex <> -1 Then
                ImportList.SetSelected(updatedItemIndex, True)
            End If
        End Sub

        ''' <summary>
        ''' The import buttons state depend on the contents of this text box
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub UserImportTextBox_TextChanged(sender As Object, e As EventArgs) Handles UserImportTextBox.TextChanged
            EnableImportGroup()
        End Sub

        Private Sub AdviseReferencesEvents(vsProject As VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso _referencesEventsCookie Is Nothing Then
                Dim projectEvents As VSLangProj.VSProjectEvents = vsProject.Events
                Dim referencesEvents As VSLangProj.ReferencesEvents = projectEvents.ReferencesEvents
                _referencesEventsCookie = New NativeMethods.ConnectionPointCookie(referencesEvents, Me, GetType(VSLangProj._dispReferencesEvents))
            End If
        End Sub

        Private Sub UnadviseReferencesEvents()
            If _referencesEventsCookie IsNot Nothing Then
                _referencesEventsCookie.Disconnect()
                _referencesEventsCookie = Nothing
            End If
        End Sub

        Private Sub AdviseImportsEvents(vsProject As VSLangProj.VSProject)
            If vsProject IsNot Nothing AndAlso _importsEventsCookie Is Nothing Then
                Dim projectEvents As VSLangProj.VSProjectEvents = vsProject.Events
                Dim importsEvents As VSLangProj.ImportsEvents = projectEvents.ImportsEvents
                _importsEventsCookie = New NativeMethods.ConnectionPointCookie(importsEvents, Me, GetType(VSLangProj._dispImportsEvents))
            End If
        End Sub

        Private Sub UnadviseImportsEvents()
            If _importsEventsCookie IsNot Nothing Then
                _importsEventsCookie.Disconnect()
                _importsEventsCookie = Nothing
            End If
        End Sub

        ' We post a message to refresh our UI later, because the project's reference list hasn't been updated when we get message from them.
        Private Sub PostRefreshReferenceListMessage()
            NativeMethods.PostMessage(Handle, WmUserConstants.WM_REFPAGE_REFERENCES_REFRESH, 0, 0)
        End Sub

        ' We post a message to refresh the imports list
        Private Sub PostRefreshImportListMessage()
            If Not _needRefreshImportList Then
                _needRefreshImportList = True
                NativeMethods.PostMessage(Handle, WmUserConstants.WM_REFPAGE_IMPORTS_REFRESH, 0, 0)
            End If
        End Sub

        ' We post a message to refresh our UI later, because the project's reference list hasn't been updated when we get message from them.
        Private Sub PostRefreshServiceReferenceListMessage()
            NativeMethods.PostMessage(Handle, WmUserConstants.WM_REFPAGE_SERVICEREFERENCES_REFRESH, 0, 0)
        End Sub

#Region "VSLangProj._dispReferencesEvents"
        ' We monitor Reference collection events to update our lists...
        Public Sub ReferenceAdded(reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceAdded
            If Not _updatingReferences Then
                If Not IsImplicitlyAddedReference(reference) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, reference)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Public Sub ReferenceChanged(reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceChanged
            If Not _updatingReferences Then
                AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, reference)
                PostRefreshReferenceListMessage()
            End If
        End Sub

        Public Sub ReferenceRemoved(reference As VSLangProj.Reference) Implements VSLangProj._dispReferencesEvents.ReferenceRemoved
            If Not _updatingReferences Then
                If Not IsImplicitlyAddedReference(reference) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, reference)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub
#End Region

#Region "VSLangProj._dispImportsEvents"
        Public Sub ImportAdded(importNamespace As String) Implements VSLangProj._dispImportsEvents.ImportAdded
            ' We always post a refresh message when the window becomes activated. So ignore it if we are not activated.
            If Not _ignoreImportEvent AndAlso IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub

        Public Sub ImportRemoved(importNamespace As String) Implements VSLangProj._dispImportsEvents.ImportRemoved
            ' We always post a refresh message when the window becomes activated. So ignore it if we are not activated.
            If Not _ignoreImportEvent AndAlso IsActivated Then
                PostRefreshImportListMessage()
            End If
        End Sub
#End Region

#Region "EnvDTE.ProjectItemsEvents"
        ' We monitor ProjectItems collection events to update our lists...
        ' We only pay attention to the WebReference items inside the project the reference page works with...
        Private WithEvents _projectItemEvents As EnvDTE.ProjectItemsEvents

        Public Sub ProjectItemEvents_ItemAdded(projectItem As EnvDTE.ProjectItem) Handles _projectItemEvents.ItemAdded
            If Not _updatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                If theVSProject.WebReferencesFolder Is projectItem.Collection.Parent AndAlso IsWebReferenceItem(projectItem) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, projectItem)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Public Sub ProjectItemEvents_ItemRemoved(projectItem As EnvDTE.ProjectItem) Handles _projectItemEvents.ItemRemoved
            If Not _updatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, projectItem)
                PostRefreshReferenceListMessage()
            End If
        End Sub

        Public Sub ProjectItemEvents_ItemRenamed(projectItem As EnvDTE.ProjectItem, oldName As String) Handles _projectItemEvents.ItemRenamed
            If Not _updatingReferences AndAlso projectItem.ContainingProject Is DTEProject Then
                Dim theVSProject As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
                If theVSProject.WebReferencesFolder Is projectItem.Collection.Parent AndAlso IsWebReferenceItem(projectItem) Then
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, projectItem)
                    PostRefreshReferenceListMessage()
                End If
            End If
        End Sub

        Private Sub AdviseWebReferencesEvents()
            If _projectItemEvents Is Nothing Then
                _projectItemEvents = CType(DTE.Events.GetObject("VBProjectItemsEvents"), EnvDTE.ProjectItemsEvents)
            End If
        End Sub

        Private Sub UnadviseWebReferencesEvents()
            _projectItemEvents = Nothing
        End Sub
#End Region

#Region "ReferenceManagerEvents"
        Private _serviceReferenceEventCookie As UInteger
        Private _serviceReferenceEventHooked As Boolean

        Private Sub AdviseServiceReferencesEvents()
            If _referenceGroupManager IsNot Nothing AndAlso Not _serviceReferenceEventHooked Then
                _referenceGroupManager.AdviseWCFReferenceEvents(Me, _serviceReferenceEventCookie)
                _serviceReferenceEventHooked = True
            End If
        End Sub

        Private Sub UnadviseServiceReferencesEvents()
            If _referenceGroupManager IsNot Nothing AndAlso _serviceReferenceEventHooked Then
                _referenceGroupManager.UnadviseWCFReferenceEvents(_serviceReferenceEventCookie)
                _serviceReferenceEventHooked = False
            End If
        End Sub

        Private Sub ServiceReference_OnReferenceGroupCollectionChanging() Implements IVsWCFReferenceEvents.OnReferenceGroupCollectionChanging
        End Sub

        Private Sub OnReferenceGroupCollectionChanged() Implements IVsWCFReferenceEvents.OnReferenceGroupCollectionChanged
            If Not _updatingReferences Then
                PostRefreshServiceReferenceListMessage()
            End If
        End Sub

        Private Sub OnMetadataChanging(pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnMetadataChanging
        End Sub

        Private Sub OnMetadataChanged(pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnMetadataChanged
        End Sub

        Private Sub OnReferenceGroupPropertiesChanging(pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnReferenceGroupPropertiesChanging
        End Sub

        Private Sub OnReferenceGroupPropertiesChanged(pReferenceGroup As IVsWCFReferenceGroup) Implements IVsWCFReferenceEvents.OnReferenceGroupPropertiesChanged
            AddDelayUpdateItem(ReferenceUpdateType.ReferenceChanged, pReferenceGroup)
            PostRefreshReferenceListMessage()
        End Sub

        Private Sub OnConfigurationChanged() Implements IVsWCFReferenceEvents.OnConfigurationChanged
        End Sub

        ''' <summary>
        ''' Reference all service references in the list.
        ''' We actually compare the original list and new list to generate DelayUpdateItem and process them later.
        ''' </summary>
        Private Sub RefreshServiceReferences()
            If _referenceGroupManager IsNot Nothing Then
                Dim collection As IVsWCFReferenceGroupCollection = _referenceGroupManager.GetReferenceGroupCollection()
                Dim newReferences As New ArrayList()
                For j As Integer = 0 To collection.Count - 1
                    newReferences.Add(collection.Item(j))
                Next
                For i As Integer = 0 To ReferenceList.Items.Count - 1
                    Dim serviceCompo As ServiceReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ServiceReferenceComponent)

                    If serviceCompo IsNot Nothing Then
                        Dim oldReference As IVsWCFReferenceGroup = serviceCompo.ReferenceGroup
                        Dim newIndex As Integer = newReferences.IndexOf(oldReference)
                        If newIndex < 0 Then
                            AddDelayUpdateItem(ReferenceUpdateType.ReferenceRemoved, oldReference)
                        Else
                            newReferences.RemoveAt(newIndex)
                        End If
                    End If
                Next

                For Each newRef As IVsWCFReferenceGroup In newReferences
                    AddDelayUpdateItem(ReferenceUpdateType.ReferenceAdded, newRef)
                Next

                ProcessDelayUpdateItems()
            End If
        End Sub
#End Region

#Region "ReferenceUpdateItem"
        Private Enum ReferenceUpdateType
            ReferenceAdded
            ReferenceChanged
            ReferenceRemoved
        End Enum

        ''' <summary>
        ''' This is the structure we used to save information when we receive Reference/WebReference change event.
        ''' We save the changes in a collection, and do a batch process to update our UI later.
        '''  We record Reference/WebReference changes with the same class. But only one of the Reference and WebReference property contains value, while the other one contains Nothing
        ''' </summary>
        Private Class ReferenceUpdateItem
            Private ReadOnly _updateType As ReferenceUpdateType
            Private ReadOnly _reference As VSLangProj.Reference
            Private ReadOnly _webReference As EnvDTE.ProjectItem
            Private ReadOnly _serviceReference As IVsWCFReferenceGroup

            Friend Sub New(updateType As ReferenceUpdateType, reference As VSLangProj.Reference)
                _updateType = updateType
                _reference = reference
            End Sub

            Friend Sub New(updateType As ReferenceUpdateType, item As EnvDTE.ProjectItem)
                _updateType = updateType
                _webReference = item
            End Sub

            Friend Sub New(updateType As ReferenceUpdateType, item As IVsWCFReferenceGroup)
                _updateType = updateType
                _serviceReference = item
            End Sub

            Friend ReadOnly Property UpdateType As ReferenceUpdateType
                Get
                    Return _updateType
                End Get
            End Property

            Friend ReadOnly Property Reference As VSLangProj.Reference
                Get
                    Return _reference
                End Get
            End Property

            Friend ReadOnly Property WebReference As EnvDTE.ProjectItem
                Get
                    Return _webReference
                End Get
            End Property

            Friend ReadOnly Property ServiceReference As IVsWCFReferenceGroup
                Get
                    Return _serviceReference
                End Get
            End Property
        End Class
#End Region

        ''' <summary>
        ''' We save information in a collection when we receive Reference change event.
        ''' </summary>
        Private Overloads Sub AddDelayUpdateItem(updateType As ReferenceUpdateType, reference As VSLangProj.Reference)
            If _delayUpdatingItems Is Nothing Then
                _delayUpdatingItems = New Queue
            End If
            _delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, reference))
        End Sub

        ''' <summary>
        ''' We save information in a collection when we receive WebReference change event.
        ''' </summary>
        Private Overloads Sub AddDelayUpdateItem(updateType As ReferenceUpdateType, item As EnvDTE.ProjectItem)
            If _delayUpdatingItems Is Nothing Then
                _delayUpdatingItems = New Queue
            End If
            _delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, item))
        End Sub

        ''' <summary>
        ''' We save information in a collection when we receive ServiceReference change event.
        ''' </summary>
        Private Overloads Sub AddDelayUpdateItem(updateType As ReferenceUpdateType, item As IVsWCFReferenceGroup)
            If _delayUpdatingItems Is Nothing Then
                _delayUpdatingItems = New Queue
            End If
            _delayUpdatingItems.Enqueue(New ReferenceUpdateItem(updateType, item))
        End Sub

        ''' <summary>
        ''' We  save information in a collection when we receive Reference/WebReference change event.
        ''' We will do a batch process to update our UI later.
        '''  In some cases, we call ProcessDelayUpdateItems to do the process after we finish the UI action.
        ''' But in most case, we post a window message, and do the process later. It prevents us to access the object when it is not ready.
        ''' </summary>
        Private Sub ProcessDelayUpdateItems()
            If _delayUpdatingItems IsNot Nothing Then
                Dim updateComponents As New ArrayList()

                ReferenceList.BeginUpdate()
                HoldSelectionChange(True)
                Try
                    While _delayUpdatingItems.Count > 0
                        Dim updateItem As ReferenceUpdateItem = CType(_delayUpdatingItems.Dequeue(), ReferenceUpdateItem)
                        If updateItem.UpdateType = ReferenceUpdateType.ReferenceAdded Then
                            ' add a new item...
                            Dim newName As String
                            Dim listViewItem As ListViewItem
                            Dim newCompo As Object

                            If updateItem.Reference IsNot Nothing Then
                                newName = updateItem.Reference.Name
                                newCompo = New ReferenceComponent(updateItem.Reference)
                                listViewItem = ReferenceToListViewItem(updateItem.Reference, newCompo)
                                Debug.Assert(Not IsImplicitlyAddedReference(updateItem.Reference), "Implicitly added references should have been filtered out beforehand")
                            ElseIf updateItem.WebReference IsNot Nothing Then
                                newName = updateItem.WebReference.Name
                                newCompo = New WebReferenceComponent(Me, updateItem.WebReference)
                                listViewItem = WebReferenceToListViewItem(updateItem.WebReference, newCompo)
                            Else
                                Debug.Assert(updateItem.ServiceReference IsNot Nothing)
                                Dim service As New ServiceReferenceComponent(_referenceGroupManager.GetReferenceGroupCollection(), updateItem.ServiceReference)
                                newName = service.[Namespace]
                                newCompo = service
                                listViewItem = ServiceReferenceToListViewItem(service)
                            End If

                            ' first -- find the right position to insert...
                            Dim i As Integer
                            For i = 0 To ReferenceList.Items.Count - 1
                                Dim curItem As ListViewItem = ReferenceList.Items(i)
                                If ReferenceList.ListViewItemSorter.Compare(curItem, listViewItem) > 0 Then
                                    Exit For
                                End If
                            Next

                            If i < ReferenceList.Items.Count Then
                                ReferenceList.Items.Insert(i, listViewItem)
                            Else
                                ReferenceList.Items.Add(listViewItem)
                            End If
                            updateComponents.Add(newCompo)
                        Else
                            ' Remove/update -- find the original item in the list first
                            For i As Integer = ReferenceList.Items.Count - 1 To 0 Step -1
                                Dim refCompo As ReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ReferenceComponent)
                                Dim webCompo As WebReferenceComponent = TryCast(ReferenceList.Items(i).Tag, WebReferenceComponent)
                                Dim serviceCompo As ServiceReferenceComponent = TryCast(ReferenceList.Items(i).Tag, ServiceReferenceComponent)

                                If refCompo IsNot Nothing AndAlso refCompo.CurrentObject Is updateItem.Reference OrElse
                                   webCompo IsNot Nothing AndAlso webCompo.WebReference Is updateItem.WebReference OrElse
                                   serviceCompo IsNot Nothing AndAlso serviceCompo.ReferenceGroup Is updateItem.ServiceReference Then
                                    If updateItem.UpdateType = ReferenceUpdateType.ReferenceRemoved Then
                                        '(Note: we don't want to call IsImplicitlyAddedReference on the reference in this case, because it has already
                                        '  been deleted and therefore that call will throw.)
                                        ReferenceList.Items.RemoveAt(i)
                                    Else
                                        ' Update -- refresh our UI if any properties changed...
                                        If refCompo IsNot Nothing Then
                                            Debug.Assert(Not IsImplicitlyAddedReference(updateItem.Reference), "Implicitly added references should have been filtered out beforehand")
                                            ReferenceList.Items(i) = ReferenceToListViewItem(updateItem.Reference, refCompo)
                                            updateComponents.Add(refCompo)
                                        ElseIf webCompo IsNot Nothing Then
                                            ReferenceList.Items(i) = WebReferenceToListViewItem(updateItem.WebReference, webCompo)
                                            updateComponents.Add(webCompo)
                                        Else
                                            Debug.Assert(serviceCompo IsNot Nothing)
                                            ReferenceList.Items(i) = ServiceReferenceToListViewItem(serviceCompo)
                                            updateComponents.Add(serviceCompo)
                                        End If
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                    End While

                    ' we will update the selection area if there is new item inserted...
                    If updateComponents.Count > 0 Then
                        Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                        indices.Clear()
                        For Each compo As Object In updateComponents
                            For j As Integer = 0 To ReferenceList.Items.Count - 1
                                If ReferenceList.Items(j).Tag Is compo Then
                                    If Not indices.Contains(j) Then
                                        indices.Add(j)
                                    End If
                                    Exit For
                                End If
                            Next
                        Next
                    End If
                    _delayUpdatingItems = Nothing
                Finally
                    ReferenceList.EndUpdate()
                    HoldSelectionChange(False)
                End Try

                ReferenceList.Refresh()
                PopulateImportsList(True)

                EnableReferenceGroup()
                PushSelection()
            End If
        End Sub

        ''' <summary>
        ''' This function will be called when the customer change the property on the propertyPage, we need update our UI as well...
        ''' </summary>
        Friend Sub OnWebReferencePropertyChanged(webReference As WebReferenceComponent)
            HoldSelectionChange(True)
            Try
                For i As Integer = 0 To ReferenceList.Items.Count - 1
                    If ReferenceList.Items(i).Tag Is webReference Then
                        ReferenceList.Items(i) = WebReferenceToListViewItem(webReference.WebReference, webReference)

                        Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                        indices.Clear()
                        indices.Add(i)

                        Exit For
                    End If
                Next
            Finally
                HoldSelectionChange(False)
            End Try

            PushSelection()
        End Sub

#Region "ISelectionContainer"
        ' This is the interface we implement to push the object to the propertyGrid...

        ' get the number of the objects in the whole collection or only selected objects
        Private Function CountObjects(flags As UInteger, ByRef pc As UInteger) As Integer Implements ISelectionContainer.CountObjects
            If flags = 1 Then   ' GETOBJS_ALL
                pc = CUInt(ReferenceList.Items.Count)
            ElseIf flags = 2 Then ' GETOBJS_SELECTED
                pc = CUInt(ReferenceList.SelectedIndices.Count)
            Else
                Return NativeMethods.E_INVALIDARG
            End If
            Return NativeMethods.S_OK
        End Function

        ' get objects in the whole collection or only selected objects
        Private Function GetObjects(flags As UInteger, cObjects As UInteger, objects As Object()) As Integer Implements ISelectionContainer.GetObjects
            If flags = 1 Then   ' GETOBJS_ALL
                For i As Integer = 0 To Math.Min(ReferenceList.Items.Count, CInt(cObjects)) - 1
                    objects(i) = ReferenceList.Items(i).Tag
                Next
            ElseIf flags = 2 Then ' GETOBJS_SELECTED
                Dim selectedItems As ListView.SelectedListViewItemCollection = ReferenceList.SelectedItems
                For i As Integer = 0 To Math.Min(selectedItems.Count, CInt(cObjects)) - 1
                    objects(i) = selectedItems.Item(i).Tag
                Next
            Else
                Return NativeMethods.E_INVALIDARG
            End If
            Return NativeMethods.S_OK
        End Function

        ' select objects -- it will be called when the customer changes selection on the dropdown box of the propertyGrid
        Private Function SelectObjects(ucSelected As UInteger, objects As Object(), flags As UInteger) As Integer Implements ISelectionContainer.SelectObjects
            HoldSelectionChange(True)
            Try
                ReferenceList.Select()

                Dim indices As ListView.SelectedIndexCollection = ReferenceList.SelectedIndices()
                indices.Clear()
                For i As Integer = 0 To CInt(ucSelected) - 1
                    For j As Integer = 0 To ReferenceList.Items.Count - 1
                        If objects(i) Is ReferenceList.Items(j).Tag Then
                            indices.Add(j)
                        End If
                    Next
                Next
            Finally
                HoldSelectionChange(False)
            End Try

            Return NativeMethods.S_OK
        End Function

#End Region

        ' This is a state when we shouldn't push the selection to the propertyGrid.
        ' We can not push the selection when the propertyGrid calls us to change the selection, and sometime, we hold it to prevent refreshing the propertyGrid when we do something...
        Private Sub HoldSelectionChange(needHold As Boolean)
            If needHold Then
                _holdSelectionChange += 1
            Else
                _holdSelectionChange -= 1
            End If
        End Sub

        ''' <summary>
        ''' Push selection to the propertyGrid
        ''' </summary>
        Private Sub PushSelection()
            If _holdSelectionChange <= 0 Then
                Dim vsTrackSelection As ITrackSelection = TrackSelection
                If vsTrackSelection IsNot Nothing Then
                    vsTrackSelection.OnSelectChange(Me)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Searches up the parent chain for an ApplicationDesignerView, if there is one.
        ''' </summary>
        ''' <returns>The PropPageUserControlBase which hosts this property page, if any, or else Nothing.</returns>
        Private Function FindPropPageDesignerView() As PropPageDesigner.PropPageDesignerView
            Dim parentWindow As Control = Parent
            While parentWindow IsNot Nothing
                If TypeOf parentWindow Is PropPageDesigner.PropPageDesignerView Then
                    Return DirectCast(parentWindow, PropPageDesigner.PropPageDesignerView)
                Else
                    parentWindow = parentWindow.Parent
                End If
            End While
            Return Nothing
        End Function

    End Class

    Friend Interface IReferenceComponent
        Function GetName() As String
        Sub Remove()
    End Interface
    Friend Interface IUpdatableReferenceComponent
        Sub Update()
    End Interface

#Disable Warning CA1067 ' This type hides Equals by name to implement IEquatable and hence can't override Equals.
    ''' <summary>
    ''' Parses and compares identities of VB Imports statements.
    ''' For XML imports, the identity is the XML namespace name (could be empty).
    ''' For VB imports, the identity is the alias name if present and the namespace itself otherwise.
    ''' </summary>
    Friend Structure ImportIdentity
#Enable Warning CA1067 ' Override Object.Equals(object) when implementing IEquatable<T>
        Implements IEquatable(Of ImportIdentity)

        Private Const AliasGroupName As String = "Alias"
        Private Const AliasGroup As String = "(?<" & AliasGroupName & ">[^=""'\s]+)"

        ' Regular expression for parsing XML imports statement (<xmlns[:Alias]='url'>).
        Private Shared ReadOnly s_xmlImportRegex As New Regex(
            "^\s*\<\s*[xX][mM][lL][nN][sS]\s*(:\s*" & AliasGroup & ")?\s*=\s*(""[^""]*""|'[^']*')\s*\>\s*$",
            RegexOptions.Compiled)

        ' Regular expression for parsing VB alias imports statement (Alias=Namespace).
        Private Shared ReadOnly s_vbImportRegex As New Regex(
            "^\s*" & AliasGroup & "\s*=\s*.*$",
            RegexOptions.Compiled)

        ' Kind of import - VB regular, VB Alias, xmlns.
        Private Enum ImportKind
            VBNamespace
            VBAlias
            XmlNamespace
        End Enum

        ' Kind of the import (see above).
        Private ReadOnly _kind As ImportKind
        ' The identity of the import used for comparison.
        Private ReadOnly _identity As String

        ''' <summary>
        ''' Creates a new instance of the <see cref="ImportIdentity"/> structure.
        ''' </summary>
        ''' <param name="import">The imports statement (without 'Imports' keyword)</param>
        Public Sub New(import As String)
            Debug.Assert(import IsNot Nothing)

            ' Trim the string to get rid of leading/trailing spaces.
            import = import.Trim()

            ' Try to match against XML imports syntax first.
            Dim m As Match = s_xmlImportRegex.Match(import)
            If m.Success Then
                ' If succeeded, set identity to the alias (namespace).
                _kind = ImportKind.XmlNamespace
                _identity = m.Groups(AliasGroupName).Value
            Else
                ' If failed, match against VB alias import syntax.
                m = s_vbImportRegex.Match(import)
                If m.Success Then
                    ' If succeeded, use alias as identity.
                    _kind = ImportKind.VBAlias
                    _identity = m.Groups(AliasGroupName).Value
                Else
                    ' Otherwise use the whole import string as identity (namespace or invalid syntax).
                    _kind = ImportKind.VBNamespace
                    _identity = import
                End If
            End If

            Debug.Assert(_identity IsNot Nothing)
        End Sub

        ''' <summary>
        ''' Returns whether this instance of <see cref="ImportIdentity"/> is the same (has same identity)
        ''' as another given imports.
        ''' </summary>
        ''' <param name="other">The imports to compare to.</param>
        ''' <returns>True if both imports are XML (both are non-XML) and identities much case-sensitive (case-insensitive).</returns>
        ''' <remarks>Checks a</remarks>
        Public Shadows Function Equals(other As ImportIdentity) As Boolean Implements IEquatable(Of ImportIdentity).Equals
            Return _kind = other._kind AndAlso
                _identity.Equals(other._identity,
                    IIf(_kind = ImportKind.XmlNamespace, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase))
        End Function
    End Structure
End Namespace
