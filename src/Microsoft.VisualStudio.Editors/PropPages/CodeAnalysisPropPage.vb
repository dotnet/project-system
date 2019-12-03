' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports NuGet.VisualStudio

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class CodeAnalysisPropPage
        Inherits PropPageUserControlBase

        Private Const NugetFeed As String = "https://packages.nuget.org/api/v2"
        Private Const FxCopAnalyzersPackageId As String = "Microsoft.CodeAnalysis.FxCopAnalyzers"
        Private Const NuGetPackageManagerPackageGuid As String = "5fcc8577-4feb-4d04-ad72-d6c629b083cc"
        Private Const NuGetPackageManagerSearchProviderGuid As String = "042C2B4B-C7F7-49DB-B7A2-402EB8DC7892"
        Private Const RoslynAnalyzersDocumentationLink As String = "https://docs.microsoft.com/visualstudio/code-quality/roslyn-analyzers-overview"

        Private Shared ReadOnly s_latestStableVersion As Version = New Version(2, 9, 6)
        Private Shared ReadOnly s_childPackageIds As HashSet(Of String) = New HashSet(Of String)(
            {"Microsoft.CodeQuality.Analyzers", "Microsoft.NetCore.Analyzers", "Microsoft.NetFramework.Analyzers"},
            StringComparer.OrdinalIgnoreCase)

        Private _packageInstallerServices As IVsPackageInstallerServices
        Private _packageInstaller As IVsPackageInstaller2
        Private _packageUninstaller As IVsPackageUninstaller
        Private _packageRestorer As IVsPackageRestorer
        Private _threadedWaitDialogFactory As IVsThreadedWaitDialogFactory
        Private _nugetPackage As IVsPackage
        Private _installedFxCopAnalyzersVersionString As String

        Public Sub New()
            MyBase.New()

            InitializeComponent()
            RefreshInstallFxCopAnalyzersButtons(reset:=True)

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub

        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()

            Dim componentModel = CType(ServiceProvider.GetService(GetType(SComponentModel)), IComponentModel)
            _packageInstallerServices = componentModel.GetExtensions(Of IVsPackageInstallerServices)().SingleOrDefault()
            _packageInstaller = componentModel.GetService(Of IVsPackageInstaller2)()
            _packageUninstaller = componentModel.GetService(Of IVsPackageUninstaller)()
            _packageRestorer = componentModel.GetService(Of IVsPackageRestorer)()
            _threadedWaitDialogFactory = CType(ServiceProvider.GetService(GetType(SVsThreadedWaitDialogFactory)), IVsThreadedWaitDialogFactory)

            Dim shell = CType(ServiceProvider.GetService(GetType(SVsShell)), IVsShell)
            Dim nugetGuid = New Guid(NuGetPackageManagerPackageGuid)
            shell?.LoadPackage(nugetGuid, _nugetPackage)

            RefreshInstalledFxCopAnalyzersVersionAndButtons()
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                        New PropertyControlData(1, "RunAnalyzersDuringBuild", RunAnalyzersDuringBuild, ControlDataFlags.None),
                        New PropertyControlData(2, "RunAnalyzersDuringLiveAnalysis", RunAnalyzersDuringLiveAnalysis, ControlDataFlags.PersistedInProjectUserFile)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

        Protected Overrides Function GetF1HelpKeyword() As String
            ' TODO: New help keyword
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

        Private Sub RefreshInstalledFxCopAnalyzersVersionAndButtons(Optional restorePackages As Boolean = False)
            If Not TryGetInstalledFxCopAnalyzersVersion(restorePackages, _installedFxCopAnalyzersVersionString) Then
                RefreshInstallFxCopAnalyzersButtons(reset:=True)
                Return
            End If

            InstalledVersionTextBox.Text = If(_installedFxCopAnalyzersVersionString, My.Resources.Strings.NotInstalledText)
            RefreshInstallFxCopAnalyzersButtons(reset:=False)
        End Sub

        Private Sub RefreshInstallFxCopAnalyzersButtons(reset As Boolean)
            If DTEProject Is Nothing Then
                reset = True
            End If

            InstallLatestVersionButton.Enabled = Not reset AndAlso Not HasFxCopAnalyzersInstalled()
            UninstallAnalyzersButton.Enabled = Not reset AndAlso HasFxCopAnalyzersInstalled()
            InstallCustomVersionButton.Enabled = Not reset AndAlso _nugetPackage IsNot Nothing

            InstallLatestVersionButton.Visible = Not HasFxCopAnalyzersInstalled()
            UninstallAnalyzersButton.Visible = Not InstallLatestVersionButton.Visible
        End Sub

        Private Function HasFxCopAnalyzersInstalled() As Boolean
            Return Not String.IsNullOrEmpty(_installedFxCopAnalyzersVersionString)
        End Function

        ' Returns False if attempt to GetInstalledPackages did not succeed
        Private Function TryGetInstalledFxCopAnalyzersVersion(restorePackages As Boolean, ByRef versionString As String) As Boolean
            versionString = Nothing

            If DTEProject Is Nothing Then
                Return False
            End If

            If restorePackages Then
                Using session = _threadedWaitDialogFactory.StartWaitDialog(My.Resources.Strings.RestoringPackagesMessage)

                    ' Temporary workaround for https://github.com/NuGet/Home/issues/8616
                    ' Add a small delay before invoking RestorePackages/GetInstalledPackages APIs.
                    Thread.Sleep(5000)

                    _packageRestorer.RestorePackages(DTEProject)

                    session.UserCancellationToken.ThrowIfCancellationRequested()
                End Using
            End If

            Dim installedPackages As IEnumerable(Of IVsPackageMetadata)
            Try
                installedPackages = _packageInstallerServices.GetInstalledPackages(DTEProject)
            Catch
                Return False
            End Try

            For Each package As IVsPackageMetadata In installedPackages
                If String.Equals(package.Id, FxCopAnalyzersPackageId, StringComparison.OrdinalIgnoreCase) OrElse
                   s_childPackageIds.Contains(package.Id) Then
                    versionString = package.VersionString
                    Exit For
                End If
            Next

            Return True
        End Function

        Private Sub InstallLatestVersionButton_Click(sender As Object, e As EventArgs) Handles InstallLatestVersionButton.Click
            Debug.Assert(DTEProject IsNot Nothing)

            Try
                _packageInstaller.InstallPackage(NugetFeed, DTEProject, FxCopAnalyzersPackageId, s_latestStableVersion, ignoreDependencies:=False)
            Catch
                _installedFxCopAnalyzersVersionString = Nothing
            End Try

            RefreshInstalledFxCopAnalyzersVersionAndButtons(restorePackages:=True)

            If _installedFxCopAnalyzersVersionString Is Nothing Then
                DesignerFramework.DesignUtil.ShowMessage(ServiceProvider, My.Resources.Strings.FxCopAnalyzersInstallFailedMessage, DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        Private Sub UninstallAnalyzersButton_Click(sender As Object, e As EventArgs) Handles UninstallAnalyzersButton.Click
            Debug.Assert(DTEProject IsNot Nothing)
            Debug.Assert(_installedFxCopAnalyzersVersionString IsNot Nothing)
            Dim versionText = _installedFxCopAnalyzersVersionString

            Try
                _packageUninstaller.UninstallPackage(DTEProject, FxCopAnalyzersPackageId, removeDependencies:=True)
            Catch
            End Try

            RefreshInstalledFxCopAnalyzersVersionAndButtons(restorePackages:=True)

            If _installedFxCopAnalyzersVersionString IsNot Nothing Then
                DesignerFramework.DesignUtil.ShowMessage(ServiceProvider, String.Format(My.Resources.Strings.FxCopAnalyzersUninstallFailedMessage, versionText), DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Sub

        Private Sub InstallCustomVersionButton_Click(sender As Object, e As EventArgs) Handles InstallCustomVersionButton.Click
            Debug.Assert(_nugetPackage IsNot Nothing)

            ' Reset and disable all the buttons as we are about to navigate
            ' away from this page to the NuGet Package manager.
            RefreshInstallFxCopAnalyzersButtons(reset:=True)

            ' We're able to launch the package manager (with an item in its search box) by
            ' using the IVsSearchProvider API that the NuGet package exposes.
            '
            ' We get that interface for it and then pass it a SearchQuery that effectively
            ' wraps the package name we're looking for. The NuGet package will then read
            ' out that string and populate their search box with it.

            Dim extensionProvider = CType(_nugetPackage, IVsPackageExtensionProvider)
            Dim extensionGuid = New Guid(NuGetPackageManagerSearchProviderGuid)
            Dim emptyGuid = Guid.Empty
            Dim searchProvider = CType(extensionProvider.CreateExtensionInstance(emptyGuid, extensionGuid), IVsSearchProvider)
            Dim task = searchProvider.CreateSearch(dwCookie:=1, pSearchQuery:=SearchQuery.FxCopAnalyzersQueryInstance, pSearchCallback:=SearchQuery.FxCopAnalyzersQueryInstance)
            task.Start()
        End Sub

        Private Sub RoslynAnalyzersLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles RoslynAnalyzersHelpLinkLabel.LinkClicked
            RoslynAnalyzersHelpLinkLabel.LinkVisited = True
            Process.Start(RoslynAnalyzersDocumentationLink)
        End Sub

        Protected Overrides Sub OnPageActivated(activated As Boolean)
            MyBase.OnPageActivated(activated)

            If activated Then
                RefreshInstalledFxCopAnalyzersVersionAndButtons()
            End If
        End Sub

        Private NotInheritable Class SearchQuery
            Implements IVsSearchQuery
            Implements IVsSearchProviderCallback

            Public Shared FxCopAnalyzersQueryInstance As SearchQuery = New SearchQuery()

            Private Sub New()
            End Sub

            Public ReadOnly Property SearchString As String Implements IVsSearchQuery.SearchString
                Get
                    Return FxCopAnalyzersPackageId
                End Get
            End Property

            Public ReadOnly Property ParseError As UInteger Implements IVsSearchQuery.ParseError
                Get
                    Return 0
                End Get
            End Property

            Public Function GetTokens(dwMaxTokens As UInteger, rgpSearchTokens As IVsSearchToken()) As UInteger Implements IVsSearchQuery.GetTokens
                Return 0
            End Function

            Public Sub ReportProgress(pTask As IVsSearchTask, dwProgress As UInteger, dwMaxProgress As UInteger) Implements IVsSearchCallback.ReportProgress, IVsSearchProviderCallback.ReportProgress
            End Sub

            Public Sub ReportComplete(pTask As IVsSearchTask, dwResultsFound As UInteger) Implements IVsSearchCallback.ReportComplete, IVsSearchProviderCallback.ReportComplete
            End Sub

            Public Sub ReportResult(pTask As IVsSearchTask, pSearchItemResult As IVsSearchItemResult) Implements IVsSearchProviderCallback.ReportResult
                pSearchItemResult.InvokeAction()
            End Sub

            Public Sub ReportResults(pTask As IVsSearchTask, dwResults As UInteger, pSearchItemResults As IVsSearchItemResult()) Implements IVsSearchProviderCallback.ReportResults
            End Sub
        End Class
    End Class

End Namespace
