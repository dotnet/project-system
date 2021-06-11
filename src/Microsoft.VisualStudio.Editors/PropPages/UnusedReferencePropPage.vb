' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Reflection.AssemblyName
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class UnusedReferencePropPage
        Inherits PropPageUserControlBase

        ' Rate to poll compiler for unused references in milliseconds 
        Private Const PollingRate As Integer = 500

        ' Whether we have generated the list
        Private _unusedReferenceListReady As Boolean

        ' Project hierarchy
        Private _hier As IVsHierarchy

        ' Compiler's reference usage provider interface
        Private _refUsageProvider As IVBReferenceUsageProvider

        ' Timer to poll compiler for unused references list
        Private _getUnusedRefsTimer As Timer

        ' The host dialog...
        Private WithEvents _hostDialog As PropPageHostDialog

        ' helper object to sort the reference list
        Private ReadOnly _referenceSorter As ListViewComparer

        ' keep the last status of the last call to GetUnusedReferences...
        ' we only update UI when the status was changed...
        Private _lastStatus As ReferenceUsageResult = ReferenceUsageResult.ReferenceUsageUnknown

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'support sorting
            _referenceSorter = New ListViewComparer()
        End Sub

#Region "Properties "
        ''' <summary>
        '''  Return true if the page can be resized...
        ''' </summary>
        Public Overrides ReadOnly Property PageResizable As Boolean
            Get
                Return True
            End Get
        End Property

#End Region

#Region "Protected Methods "
        Protected Overrides Function GetF1HelpKeyword() As String

            Return HelpKeywords.VBProjPropUnusedReference

        End Function

        ''' <summary>
        ''' ;PreApplyPageChanges
        ''' Applies property page by removing all checked references.
        ''' </summary>
        ''' <remarks>Called by ApplyPageChanges so project is in batch edit mode.</remarks>
        Protected Overrides Sub PreApplyPageChanges()

            ' Remove all checked references.
            RemoveCheckedRefs()

        End Sub

        ''' <summary>
        ''' ;OnParentChanged
        ''' We need hook up events from the hosting dialog
        ''' </summary>
        ''' <remarks>Called by ApplyPageChanges so project is in batch edit mode.</remarks>
        Protected Overrides Sub OnParentChanged(e As EventArgs)
            _hostDialog = TryCast(ParentForm, PropPageHostDialog)
            If _hostDialog IsNot Nothing Then
                With _hostDialog
                    AddHandler .FormClosed, AddressOf OnHostDialogFormClosed
                End With
            End If

        End Sub

#End Region

#Region "Private Methods "
        ''' <summary>
        ''' ;InitDialog
        ''' Initialize proppage for use on a PropPageHostDialog: 
        ''' Initialize proppage variables and install custom dialog event handlers.
        ''' </summary>
        Private Sub InitDialog()
            Try
                ' Get our project hierarchy
                _hier = ProjectHierarchy

                ' Get reference usage provider interface
                _refUsageProvider = CType(ServiceProvider.GetService(NativeMethods.VBCompilerGuid), IVBReferenceUsageProvider)
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(InitDialog), NameOf(UnusedReferencePropPage))
                Throw
            End Try

            ' Disable remove button and clear IsDirty
            _unusedReferenceListReady = False
            EnableRemoveRefs(False)

            ' Begin requesting unused references
            BeginGetUnusedRefs()

        End Sub

        ''' <summary>
        ''' ;Abort
        ''' Abort requesting unused references from compiler.
        ''' </summary>
        Private Sub Abort()

            ' Stop polling
            _getUnusedRefsTimer.Stop()

            ' End get unused references operation
            _refUsageProvider.StopGetUnusedReferences(_hier)

            ' Reset mouse cursor
            Debug.Assert(ParentForm IsNot Nothing)
            ParentForm.Cursor = Cursors.Default
        End Sub

        ''' <summary>
        ''' GetReferenceList
        ''' get list of project's references as VSLangProj.References 
        ''' </summary>
        ''' <remarks>RefsList should always be set before using this proppage</remarks>
        Private Function GetReferenceList() As ArrayList
            Dim theVSProject As VSLangProj.VSProject
            Dim ReferenceCount As Integer
            Dim refsList As ArrayList

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            ReferenceCount = theVSProject.References.Count

            refsList = New ArrayList(ReferenceCount)

            For refIndex As Integer = 0 To ReferenceCount - 1
                Dim ref As VSLangProj.Reference = theVSProject.References.Item(refIndex + 1) '1-based

                ' Don't consider implicitly added references because they cannot be removed
                ' from the VB project
                If Not IsImplicitlyAddedReference(ref) Then
                    refsList.Add(ref)
                End If
            Next refIndex

            Return refsList
        End Function

        ''' <summary>
        ''' ;EnableRemoveRefs
        ''' Enable/disable remove button and set whether there 
        ''' are changes to apply (IsDirty).
        ''' </summary>
        ''' <param name="enabled">Enable or disable</param>
        ''' <remarks>Use when proppage is on a PropPageHostDialog</remarks>
        Private Sub EnableRemoveRefs(enabled As Boolean)

            If ParentForm IsNot Nothing Then
                Debug.Assert(TypeOf ParentForm Is PropPageHostDialog, "Unused references list should be on host dialog")

                Dim RemoveButton As Button = CType(ParentForm, PropPageHostDialog).OK

                ' Enable/Disable group
                RemoveButton.Enabled = enabled

                ' indicate if we have references to remove on apply()
                If enabled Then
                    SetDirty(Me)
                    RemoveButton.DialogResult = DialogResult.OK
                Else
                    ClearIsDirty()
                End If
            End If

        End Sub

        ''' <summary>
        ''' ;UpdateStatus
        ''' Sets the proppage appearance according to current operation
        ''' </summary>
        ''' <param name="Status">Current status of proppage (equivalent to status of GetUnusedRefsList call)</param>
        Private Sub UpdateStatus(Status As ReferenceUsageResult)

            ' Only update status when necessary
            If Status <> _lastStatus Then
                ' Remember last status set
                _lastStatus = Status

                ' Use a arrow and hourglass cursor if waiting
                Debug.Assert(ParentForm IsNot Nothing)
                If Status = ReferenceUsageResult.ReferenceUsageWaiting Then
                    ParentForm.Cursor = Cursors.AppStarting
                Else
                    ParentForm.Cursor = Cursors.Default
                End If

                ' Are there any unused references?
                If Status = ReferenceUsageResult.ReferenceUsageOK AndAlso _unusedReferenceListReady AndAlso
                    UnusedReferenceList IsNot Nothing AndAlso UnusedReferenceList.Items.Count > 0 Then
                    ' Do initial enabling of remove button
                    EnableRemoveRefs(True)
                Else
                    Dim StatusText As String

                    ' Get a status string
                    Select Case Status
                        Case ReferenceUsageResult.ReferenceUsageOK
                            StatusText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceNoUnusedReferences
                        Case ReferenceUsageResult.ReferenceUsageWaiting
                            StatusText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceCompileWaiting
                        Case ReferenceUsageResult.ReferenceUsageCompileFailed
                            StatusText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceCompileFail
                        Case ReferenceUsageResult.ReferenceUsageError
                            StatusText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceError
                        Case Else
                            Debug.Fail("Unexpected status")
                            StatusText = ""
                    End Select

                    ' Use listview to display status text
                    With UnusedReferenceList
                        .BeginUpdate()

                        ' Add status text without a check box to 2nd ("Reference Name") column
                        .CheckBoxes = False
                        .Items.Clear()
                        .Items.Add("").SubItems.Add(StatusText)

                        ' Autosized listview columns 
                        SetReferenceListColumnWidths()

                        .EndUpdate()
                    End With

                    ' Disable remove button
                    EnableRemoveRefs(False)
                End If
            End If

        End Sub

        ''' <summary>
        ''' ;UpdateUnusedReferenceList
        ''' Adds a ListViewItem for each reference in UnusedRefsList.
        ''' </summary>
        ''' <param name="UnusedRefsList"> a list of VSLangProj.Reference object.</param>
        ''' <remarks>Uses ReferencePropPage.ReferenceToListViewItem to extract reference properties</remarks>
        Private Sub UpdateUnusedReferenceList(UnusedRefsList As ArrayList)

            ' Add all unused references to list view
            UnusedReferenceList.BeginUpdate()

            Try
                Dim lviRef As ListViewItem

                ' Set checkboxes and clear before adding checked items, otherwise they become unchecked
                UnusedReferenceList.CheckBoxes = True
                UnusedReferenceList.Items.Clear()

                For refIndex As Integer = 0 To UnusedRefsList.Count - 1
                    ' Convert VSLangProj.Reference to formatted list view item and insert into references list
                    lviRef = ReferencePropPage.ReferenceToListViewItem(
                        CType(UnusedRefsList(refIndex), VSLangProj.Reference), UnusedRefsList(refIndex))

                    lviRef.Checked = True

                    UnusedReferenceList.Items.Add(lviRef)
                Next

                ' We need reset order every time the dialog pops up
                UnusedReferenceList.ListViewItemSorter = _referenceSorter
                _referenceSorter.SortColumn = 0
                _referenceSorter.Sorting = SortOrder.Ascending
                UnusedReferenceList.Sorting = SortOrder.Ascending

                UnusedReferenceList.Sort()

                ' Resize columns
                SetReferenceListColumnWidths()
            Finally
                UnusedReferenceList.EndUpdate()
            End Try

        End Sub

        Private Sub BeginGetUnusedRefs()

            ' Get a new timer
            _getUnusedRefsTimer = New Timer With {
                .Interval = PollingRate
            }
            AddHandler _getUnusedRefsTimer.Tick, AddressOf OnGetUnusedRefsTimerTick

            _lastStatus = ReferenceUsageResult.ReferenceUsageUnknown

            ' Begin requesting unused references
            _getUnusedRefsTimer.Start()

        End Sub

        ''' <summary>
        ''' ;GetUnusedRefs
        ''' Poll compiler for unused references list from compiler and update listview
        ''' when received.
        ''' </summary>
        Private Sub GetUnusedRefs()

            ' Request unused references from 
            Dim UnusedRefPathsString As String = Nothing
            Dim Result As ReferenceUsageResult =
                _refUsageProvider.GetUnusedReferences(_hier, UnusedRefPathsString)

            Try
                If Result <> ReferenceUsageResult.ReferenceUsageWaiting Then
                    ' Stop polling
                    _getUnusedRefsTimer.Stop()
                End If

                If Result = ReferenceUsageResult.ReferenceUsageOK Then
                    Using New WaitCursor
                        ' Clear unused references list
                        Dim UnusedRefsList As New ArrayList

                        ' Split string of reference paths into array and iterate
                        Dim UnusedRefPaths As String() = UnusedRefPathsString.Split(ChrW(0))

                        If UnusedRefPaths.Length > 0 Then
                            Dim pathHash As New Hashtable()
                            Dim assemblyNameHash As Hashtable = Nothing

                            Dim RefsList As ArrayList = GetReferenceList()

                            ' Compare paths first.  This is a better match, since libs on disk
                            ' can be out of sync with the source for proj to proj references
                            ' and it is much faster. We should prevent calling GetAssemblyName, because it is very slow.

                            ' Prepare a hashtable for quick match
                            For iRef As Integer = 0 To RefsList.Count - 1 Step 1
                                Dim RefPath As String = CType(RefsList(iRef), VSLangProj.Reference).Path
                                If RefPath <> "" Then
                                    pathHash.Add(RefPath.ToUpper(Globalization.CultureInfo.InvariantCulture), iRef)
                                End If
                            Next

                            For Each UnusedRefPath As String In UnusedRefPaths
                                If UnusedRefPath.Length > 0 Then

                                    Dim formattedPath As String = UnusedRefPath.ToUpper(Globalization.CultureInfo.InvariantCulture)
                                    Dim refObj As Object = pathHash.Item(formattedPath)

                                    If refObj IsNot Nothing Then
                                        UnusedRefsList.Add(RefsList(CInt(refObj)))
                                        ' remove the one we matched, so we don't scan it and waste time to GetAssemblyName again...
                                        pathHash.Remove(formattedPath)
                                    ElseIf IO.File.Exists(UnusedRefPath) Then
                                        ' If we haven't matched any path, we need collect the assembly name and use it to do match...
                                        Dim UnusedRefName As String = GetAssemblyName(UnusedRefPath).FullName
                                        If UnusedRefName <> "" Then
                                            UnusedRefName = UnusedRefName.ToUpper(Globalization.CultureInfo.InvariantCulture)
                                            If assemblyNameHash Is Nothing Then
                                                assemblyNameHash = New Hashtable()
                                            End If
                                            assemblyNameHash.Add(UnusedRefName, Nothing)
                                        End If
                                    End If
                                End If
                            Next

                            If assemblyNameHash IsNot Nothing Then
                                ' try to match assemblyName...
                                For Each pathItem As DictionaryEntry In pathHash
                                    Dim RefPath As String = CStr(pathItem.Key)
                                    Dim iRef As Integer = CInt(pathItem.Value)
                                    If IO.File.Exists(RefPath) Then
                                        Dim assemblyName As System.Reflection.AssemblyName = GetAssemblyName(RefPath)
                                        If assemblyName IsNot Nothing Then
                                            Dim RefName As String = assemblyName.FullName.ToUpper(Globalization.CultureInfo.InvariantCulture)
                                            If assemblyNameHash.Contains(RefName) Then
                                                UnusedRefsList.Add(RefsList(iRef))
#If DEBUG Then
                                                assemblyNameHash.Item(RefName) = RefName
#End If
                                            End If
                                        End If
                                    End If
                                Next

#If DEBUG Then
                                For Each UnusedItem As DictionaryEntry In assemblyNameHash
                                    If UnusedItem.Value Is Nothing Then
                                        Debug.Fail("Could not find unused reference " & CStr(UnusedItem.Key))
                                    End If
                                Next
#End If
                            End If
                        End If

                        ' Update listview
                        UpdateUnusedReferenceList(UnusedRefsList)

                        _unusedReferenceListReady = True
                    End Using
                End If

            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(GetUnusedRefs), NameOf(UnusedReferencePropPage))
                Result = ReferenceUsageResult.ReferenceUsageError
            Finally
                ' Report status
                UpdateStatus(Result)
            End Try

        End Sub

        ''' <summary>
        ''' ;RemoveCheckRefs
        ''' Remove all references the user checked from the project.
        ''' </summary>
        Private Sub RemoveCheckedRefs()

            Dim ref As VSLangProj.Reference

            Dim ProjectReloaded As Boolean
            CheckoutProjectFile(ProjectReloaded)
            If ProjectReloaded Then
                Return
            End If

            Dim checkedItems As ListView.CheckedListViewItemCollection = UnusedReferenceList.CheckedItems
            If checkedItems.Count > 0 Then
                Using New WaitCursor
                    Dim batchEdit As ProjectBatchEdit = Nothing
                    If checkedItems.Count > 1 Then
                        batchEdit = New ProjectBatchEdit(_hier)
                    End If
                    Using batchEdit
                        ' Iterate all checked references to remove
                        For Each RefListItem As ListViewItem In UnusedReferenceList.CheckedItems
                            ref = DirectCast(RefListItem.Tag, VSLangProj.Reference)

                            ' Remove from project references
                            Debug.Assert(ref IsNot Nothing, "How did we get a bad reference object?")
                            ref.Remove()
                        Next
                    End Using
                End Using
            End If

        End Sub

        ''' <summary>
        ''' ;SetReferenceListColumnWidths
        ''' The Listview class does not support individual column widths, so we have to do it via sendmessage
        ''' </summary>
        ''' <remarks>Depends on ReferencePropPage.SetReferenceListColumnWidths</remarks>
        Private Sub SetReferenceListColumnWidths()

            UnusedReferenceList.View = View.Details

            ' Use ReferencePropPage's helper function for the common columns.
            ReferencePropPage.SetReferenceListColumnWidths(Me, UnusedReferenceList, 0)

        End Sub

#Region "Event Handlers "

        Private Sub OnUnusedReferenceListItemCheck(sender As Object, e As ItemCheckEventArgs) Handles UnusedReferenceList.ItemCheck

            ' Since CheckIndices is updated after this event, we enable remove button if
            ' there are more than one check references or there are none and one is being checked
            EnableRemoveRefs(e.NewValue = CheckState.Checked OrElse UnusedReferenceList.CheckedIndices.Count > 1)

        End Sub

        ''' <summary>
        '''  When the customer clicks a column header, we should sort the reference list
        ''' </summary>
        ''' <param name="sender">Event args</param>
        ''' <param name="e">Event args</param>
        Private Sub OnUnusedReferenceListColumnClick(sender As Object, e As ColumnClickEventArgs) Handles UnusedReferenceList.ColumnClick
            ListViewComparer.HandleColumnClick(UnusedReferenceList, _referenceSorter, e)
        End Sub

        Private Sub OnGetUnusedRefsTimerTick(sender As Object, e As EventArgs)

            ' Poll compiler
            GetUnusedRefs()

        End Sub

        ''' <summary>
        '''  We need initialize the dialog when it pops up (every time)
        ''' </summary>
        ''' <param name="sender">Event args</param>
        ''' <param name="e">Event args</param>
        Private Sub OnHostDialogShown(sender As Object, e As EventArgs) Handles _hostDialog.Shown

            With CType(sender, PropPageHostDialog)
                ' Set dialog appearance
                .OK.Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_UnusedReferenceRemoveButton

                ' Allow dialog to be resized
                .FormBorderStyle = FormBorderStyle.Sizable

                ' Clean up the list: We don't want to see old list refreshes when we open the dialog again.
                UnusedReferenceList.Items.Clear()

                ' Suppress column headers until something is added
                UnusedReferenceList.View = View.LargeIcon
            End With

            InitDialog()

        End Sub

        Private Sub OnHostDialogFormClosed(sender As Object, e As FormClosedEventArgs)

            ' Stop getting unused references list
            Abort()

            ' Clean up the list
            UnusedReferenceList.Items.Clear()
        End Sub

#End Region

#End Region

    End Class

End Namespace

