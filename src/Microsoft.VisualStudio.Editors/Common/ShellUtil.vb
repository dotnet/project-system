' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Imports EnvDTE

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' Utilities relating to the Visual Studio shell, services, etc.
    ''' </summary>
    Friend NotInheritable Class ShellUtil

        ''' <summary>
        ''' Gets a color from the shell's color service.  If for some reason this fails, returns the supplied
        '''   default color.
        ''' </summary>
        ''' <param name="VsUIShell">The IVsUIShell interface that must also implement IVsUIShell2 (if not, or if Nothing, default color is returned)</param>
        ''' <param name="VsSysColorIndex">The color index to look up.</param>
        ''' <param name="DefaultColor">The default color to return if the call fails.</param>
        Public Shared Function GetColor(VsUIShell As IVsUIShell, VsSysColorIndex As __VSSYSCOLOREX, DefaultColor As Color) As Color
            Return GetColor(TryCast(VsUIShell, IVsUIShell2), VsSysColorIndex, DefaultColor)
        End Function

        ''' <summary>
        ''' Gets a color from the shell's color service.  If for some reason this fails, returns the supplied
        '''   default color.
        ''' </summary>
        ''' <param name="VsUIShell2">The IVsUIShell2 interface to use (if Nothing, default color is returned)</param>
        ''' <param name="VsSysColorIndex">The color index to look up.</param>
        ''' <param name="DefaultColor">The default color to return if the call fails.</param>
        Public Shared Function GetColor(VsUIShell2 As IVsUIShell2, VsSysColorIndex As __VSSYSCOLOREX, DefaultColor As Color) As Color
            If VsUIShell2 IsNot Nothing Then
                Dim abgrValue As UInteger
                Dim Hr As Integer = VsUIShell2.GetVSSysColorEx(VsSysColorIndex, abgrValue)
                If VSErrorHandler.Succeeded(Hr) Then
                    Return COLORREFToColor(abgrValue)
                End If
            End If

            Debug.Fail("Unable to get color from the shell, using a predetermined default color instead." & vbCrLf & "Color Index = " & VsSysColorIndex & ", Default Color = &h" & Hex(DefaultColor.ToArgb))
            Return DefaultColor
        End Function

        ''' <summary>
        ''' Converts a COLORREF value (as UInteger) to System.Drawing.Color
        ''' </summary>
        ''' <param name="abgrValue">The UInteger COLORREF value</param>
        ''' <returns>The System.Drawing.Color equivalent.</returns>
        Private Shared Function COLORREFToColor(abgrValue As UInteger) As Color
            Return Color.FromArgb(CInt(abgrValue And &HFFUI), CInt((abgrValue And &HFF00UI) >> 8), CInt((abgrValue And &HFF0000UI) >> 16))
        End Function

        ''' <summary>
        ''' Retrieves the window that should be used as the owner of all dialogs and messageboxes.
        ''' </summary>
        Friend Shared Function GetDialogOwnerWindow(serviceProvider As IServiceProvider) As IWin32Window
            Dim dialogOwner As IWin32Window = Nothing
            Dim UIService As IUIService = DirectCast(serviceProvider.GetService(GetType(IUIService)), IUIService)
            If UIService IsNot Nothing Then
                dialogOwner = UIService.GetDialogOwnerWindow()
            End If

            Debug.Assert(dialogOwner IsNot Nothing, "Couldn't get DialogOwnerWindow")
            Return dialogOwner
        End Function

        ''' <summary>
        ''' Given an IVsCfg, get its configuration and platform names.
        ''' </summary>
        ''' <param name="Config">The IVsCfg to get the configuration and platform name from.</param>
        ''' <param name="ConfigName">[out] The configuration name.</param>
        ''' <param name="PlatformName">[out] The platform name.</param>
        Public Shared Sub GetConfigAndPlatformFromIVsCfg(Config As IVsCfg, ByRef ConfigName As String, ByRef PlatformName As String)
            Dim DisplayName As String = Nothing

            VSErrorHandler.ThrowOnFailure(Config.get_DisplayName(DisplayName))
            Debug.Assert(DisplayName IsNot Nothing AndAlso DisplayName <> "")

            'The configuration name and platform name are separated by a vertical bar.  The configuration
            '  part is the only portion that is user-defined.  Although the shell doesn't allow vertical bar
            '  in the configuration name, let's not take chances, so we'll find the last vertical bar in the
            '  string.
            Dim IndexOfBar As Integer = DisplayName.LastIndexOf("|"c)
            If IndexOfBar = 0 Then
                'It is possible that some old projects' configurations may not have the platform in the name.
                '  In this case, the correct thing to do is assume the platform is "Any CPU"
                ConfigName = DisplayName
                PlatformName = "Any CPU"
            Else
                ConfigName = DisplayName.Substring(0, IndexOfBar)
                PlatformName = DisplayName.Substring(IndexOfBar + 1)
            End If

            Debug.Assert(ConfigName <> "" AndAlso PlatformName <> "")
        End Sub

        ''' <summary>
        ''' Returns whether or not we're in simplified config mode for this project, which means that
        '''   we hide the configuration/platform comboboxes.
        ''' </summary>
        ''' <param name="ProjectHierarchy">The hierarchy to check</param>
        Public Shared Function GetIsSimplifiedConfigMode(ProjectHierarchy As IVsHierarchy) As Boolean
            Try
                If ProjectHierarchy IsNot Nothing Then
                    Dim Project As Project = DTEProjectFromHierarchy(ProjectHierarchy)
                    If Project IsNot Nothing Then
                        Return CanHideConfigurationsForProject(ProjectHierarchy) AndAlso Not ToolsOptionsShowAdvancedBuildConfigurations(Project.DTE)
                    End If
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, "Exception determining if we're in simplified configuration mode - default to advanced configs mode", NameOf(ShellUtil))
            End Try

            Return False 'Default to advanced configs
        End Function

        ''' <summary>
        ''' Returns whether it's permissible to hide configurations for this project.  This should normally
        '''   be returned as true until the user changes any of the default configurations (i.e., adds, deletes
        '''   or removes a configuration, at which point the project wants to show the advanced settings
        '''   from then on out).
        ''' </summary>
        ''' <param name="ProjectHierarchy">The project hierarchy to check</param>
        Private Shared Function CanHideConfigurationsForProject(ProjectHierarchy As IVsHierarchy) As Boolean
            Dim ReturnValue As Boolean = False 'If failed to get config value, default to not hiding configs

            Dim ConfigProviderObject As Object = Nothing
            Dim ConfigProvider As IVsCfgProvider2 = Nothing
            If VSErrorHandler.Succeeded(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, ConfigProviderObject)) Then
                ConfigProvider = TryCast(ConfigProviderObject, IVsCfgProvider2)
            End If

            If ConfigProvider IsNot Nothing Then
                Dim ValueObject As Object = Nothing

                'Ask the project system if configs can be hidden
                Dim hr As Integer = ConfigProvider.GetCfgProviderProperty(__VSCFGPROPID2.VSCFGPROPID_HideConfigurations, ValueObject)

                If VSErrorHandler.Succeeded(hr) AndAlso TypeOf ValueObject Is Boolean Then
                    ReturnValue = CBool(ValueObject)
                Else
                    Debug.Fail("Failed to get VSCFGPROPID_HideConfigurations from project config provider")
                    ReturnValue = False
                End If
            End If

            Return ReturnValue
        End Function

        ''' <summary>
        ''' Retrieves the current value of the "Show Advanced Build Configurations" options in
        '''   Tools.Options.
        ''' </summary>
        ''' <param name="DTE">The DTE extensibility object</param>
        Private Shared Function ToolsOptionsShowAdvancedBuildConfigurations(DTE As DTE) As Boolean
            Dim ShowValue As Boolean
            Dim ProjAndSolutionProperties As Properties
            Const EnvironmentCategory As String = "Environment"
            Const ProjectsAndSolution As String = "ProjectsandSolution"

            Try
                ProjAndSolutionProperties = DTE.Properties(EnvironmentCategory, ProjectsAndSolution)
                If ProjAndSolutionProperties IsNot Nothing Then
                    ShowValue = CBool(ProjAndSolutionProperties.Item("ShowAdvancedBuildConfigurations").Value)
                Else
                    Debug.Fail("Couldn't get ProjAndSolutionProperties property from DTE.Properties")
                    ShowValue = True 'If can't get to the property, assume advanced mode
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, "Couldn't get ShowAdvancedBuildConfigurations property from tools.options", NameOf(ShellUtil))
                Return True 'default to showing advanced
            End Try

            Return ShowValue
        End Function

        ''' <summary>
        ''' Given an IVsHierarchy, fetch the DTE Project for it, if it exists.  For project types that 
        '''   don't support this, returns Nothing (e.g. C++).
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        Public Shared Function DTEProjectFromHierarchy(ProjectHierarchy As IVsHierarchy) As Project
            If ProjectHierarchy Is Nothing Then
                Return Nothing
            End If

            Dim hr As Integer
            Dim Obj As Object = Nothing
            hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, Obj)
            If VSErrorHandler.Succeeded(hr) Then
                Return TryCast(Obj, Project)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a DTE Project, get the hierarchy corresponding to it.
        ''' </summary>
        ''' <param name="sp"></param>
        ''' <param name="project"></param>
        Public Shared Function VsHierarchyFromDTEProject(sp As IServiceProvider, project As Project) As IVsHierarchy
            Debug.Assert(sp IsNot Nothing)
            If sp Is Nothing OrElse project Is Nothing Then
                Return Nothing
            End If

            Dim vssolution As IVsSolution = TryCast(sp.GetService(GetType(IVsSolution)), IVsSolution)
            If vssolution IsNot Nothing Then
                Dim hierarchy As IVsHierarchy = Nothing
                If VSErrorHandler.Succeeded(vssolution.GetProjectOfUniqueName(project.UniqueName, hierarchy)) Then
                    Return hierarchy
                Else
                    Debug.Fail("Why didn't we get the hierarchy from the project?")
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Returns the IVsCfgProvider2 for the given project hierarchy
        ''' </summary>
        ''' <param name="ProjectHierarchy"></param>
        Public Shared Function GetConfigProvider(ProjectHierarchy As IVsHierarchy) As IVsCfgProvider2
            'CONSIDER: This will not work for all project types because they do not support this property.
            Dim ConfigProvider As Object = Nothing
            If VSErrorHandler.Failed(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, ConfigProvider)) Then
                Return Nothing
            End If
            Return TryCast(ConfigProvider, IVsCfgProvider2)
        End Function

        ''' <summary>
        ''' Given a hierarchy, determine if this is a devices project...
        ''' </summary>
        ''' <param name="hierarchy"></param>
        Public Shared Function IsDeviceProject(hierarchy As IVsHierarchy) As Boolean
            If hierarchy Is Nothing Then
                Debug.Fail("I can't determine if this is a devices project from a NULL hierarchy!?")
                Return False
            End If

            Dim vsdProperty As Object = Nothing
            Dim hr As Integer = hierarchy.GetProperty(VSITEMID.ROOT, 8000, vsdProperty)
            If Interop.NativeMethods.Succeeded(hr) AndAlso vsdProperty IsNot Nothing AndAlso TryCast(vsdProperty, IVSDProjectProperties) IsNot Nothing Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Is this a Venus project?
        ''' </summary>
        ''' <param name="hierarchy"></param>
        ''' <returns>true if it is a venus project</returns>
        Friend Shared Function IsVenusProject(hierarchy As IVsHierarchy) As Boolean

            If hierarchy Is Nothing Then
                Return False
            End If

            Try
                Dim project As Project = DTEProjectFromHierarchy(hierarchy)

                If project Is Nothing Then
                    Return False
                End If

                If String.Equals(project.Kind, VsWebSite.PrjKind.prjKindVenusProject, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(IsVenusProject), NameOf(ShellUtil))
                ' We failed. Assume that this isn't a web project...
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Is this a web (Venus WSP or WAP project)
        ''' </summary>
        ''' <param name="hierarchy"></param>
        Friend Shared Function IsWebProject(hierarchy As IVsHierarchy) As Boolean
            Const WebAppProjectGuid As String = "{349c5851-65df-11da-9384-00065b846f21}"

            If hierarchy Is Nothing Then
                Return False
            End If

            Try
                If IsVenusProject(hierarchy) Then
                    Return True
                End If

                ' VS WAP Projects are traditional vb/c# apps, but 'flavored' to add functionality
                ' for ASP.Net.  This flavoring is marked by adding a guid to the AggregateProjectType guids
                ' Get the project type guid list
                Dim guidList As New List(Of Guid)

                Dim WAPGuid As New Guid(WebAppProjectGuid)

                Dim aggregatableProject As IVsAggregatableProject = TryCast(hierarchy, IVsAggregatableProject)
                If aggregatableProject IsNot Nothing Then
                    Dim guidStrings As String = Nothing
                    '  The project guids string looks like "{Guid 1};{Guid 2};...{Guid n}" with Guid n the inner most
                    aggregatableProject.GetAggregateProjectTypeGuids(guidStrings)

                    For Each guidString As String In guidStrings.Split(New Char() {";"c})
                        If guidString <> "" Then
                            ' Insert Guid to the front
                            Try
                                Dim flavorGuid As New Guid(guidString)
                                If WAPGuid.Equals(flavorGuid) Then
                                    Return True
                                End If
                            Catch ex As Exception When ReportWithoutCrash(ex, $"We received a broken guid string from IVsAggregatableProject '{guidStrings}'", NameOf(ShellUtil))
                            End Try
                        End If
                    Next
                Else
                    '  Should not happen, but if they decide to make this project type non-flavored.
                    Dim typeGuid As Guid = Nothing
                    VSErrorHandler.ThrowOnFailure(hierarchy.GetGuidProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_TypeGuid, typeGuid))
                    If Equals(WAPGuid, typeGuid) Then
                        Return True
                    End If
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(IsWebProject), NameOf(ShellUtil))
                ' We failed. Assume that this isn't a web project...
            End Try
            Return False
        End Function
        ''' <param name="fileName">IN: name of the file to get the document info from</param>
        ''' <param name="rdt">IN: Running document table to find the info in</param>
        ''' <param name="hierarchy">OUT: Hierarchy that the document was found in</param>
        ''' <param name="itemid">OUT: Found itemId</param>
        ''' <param name="readLocks">OUT: Number of read locks for the document</param>
        ''' <param name="editLocks">OUT: Number of edit locks on the document</param>
        ''' <param name="docCookie">OUT: A cookie for the doc, 0 if the doc isn't found in the RDT</param>
        Friend Shared Sub GetDocumentInfo(fileName As String, rdt As IVsRunningDocumentTable, ByRef hierarchy As IVsHierarchy, ByRef readLocks As UInteger, ByRef editLocks As UInteger, ByRef itemid As UInteger, ByRef docCookie As UInteger)
            Requires.NotNull(fileName, NameOf(fileName))
            Requires.NotNull(rdt, NameOf(rdt))

            '
            ' Initialize out parameters...
            '
            readLocks = 0
            editLocks = 0
            itemid = VSITEMID.NIL
            docCookie = 0
            hierarchy = Nothing

            ' Now, look in the RDT to see if this doc data already has an edit lock on it.
            ' if it does, we keep it and we begin tracking changes.  Otherwise, we
            ' let it get disposed.
            '
            Dim flags As UInteger
            Dim localPunk As IntPtr = IntPtr.Zero
            Dim localFileName As String = Nothing

            Try
                VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), fileName, hierarchy, itemid, localPunk, docCookie))
            Finally
                If localPunk <> IntPtr.Zero Then
                    System.Runtime.InteropServices.Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            Try
                VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hierarchy, itemid, localPunk))
            Finally
                If localPunk <> IntPtr.Zero Then
                    System.Runtime.InteropServices.Marshal.Release(localPunk)
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Get the name of a project item as well as a SFG generated child item (if any)
        ''' Used in order to check out all dependent files for a project item
        ''' </summary>
        ''' <param name="projectitem">The parent project item that is to be checked out</param>
        ''' <param name="suffix">Suffix added by the single file generator</param>
        ''' <param name="requireExactlyOneChild">
        ''' Only add the child item to the list of items to check out if there is exactly one child
        ''' project item.
        ''' </param>
        ''' <param name="exclude">
        ''' Predicate used to filter items that we don't want to check out.
        ''' The predicate is passed each full path to the project item, and if it returns
        ''' true, the item will not be added to the list of items to check out.
        ''' </param>
        ''' <returns>
        ''' The list of items that are to be checked out
        ''' </returns>
        Friend Shared Function FileNameAndGeneratedFileName(projectitem As ProjectItem,
                                                            Optional suffix As String = ".Designer",
                                                            Optional requireExactlyOneChild As Boolean = True,
                                                            Optional exclude As Predicate(Of String) = Nothing) _
                               As List(Of String)

            Dim result As New List(Of String)

            If projectitem IsNot Nothing AndAlso projectitem.Name <> "" Then
                result.Add(DTEUtils.FileNameFromProjectItem(projectitem))
            End If

            ' For each child, check if the name matches the filename for the generated file
            If projectitem IsNot Nothing AndAlso projectitem.ProjectItems IsNot Nothing Then
                ' If we require exactly one child, we better check the number of children
                ' and bail if more than one child.
                If projectitem.ProjectItems.Count = 1 OrElse Not requireExactlyOneChild Then
                    For childNo As Integer = 1 To projectitem.ProjectItems.Count
                        Try
                            Dim childItemName As String = DTEUtils.FileNameFromProjectItem(projectitem.ProjectItems.Item(childNo))

                            ' Make sure that the filename matches what we expect.
                            If String.Equals(
                                IO.Path.GetFileNameWithoutExtension(childItemName),
                                IO.Path.GetFileNameWithoutExtension(DTEUtils.FileNameFromProjectItem(projectitem)) & suffix,
                                StringComparison.OrdinalIgnoreCase) _
                            Then
                                ' If we've got a filter predicate, we remove anything that we've been
                                ' told we shouldn't check out...
                                Dim isExcluded As Boolean = exclude IsNot Nothing AndAlso exclude.Invoke(childItemName)
                                If Not isExcluded Then
                                    result.Add(childItemName)
                                End If
                            End If
                        Catch ex As ArgumentException
                            ' If the child name wasn't a file moniker, then we may throw an argument exception here...
                            '
                            ' Don't really care about that scenario!
                        End Try
                    Next
                End If
            End If

            Return result

        End Function

        '''<summary>
        ''' a fake IVSDProjectProperties definition. We only use this to check whether the project supports this interface, but don't pay attention to the detail.
        '''</summary>
        <System.Runtime.InteropServices.ComImport, System.Runtime.InteropServices.ComVisible(False), System.Runtime.InteropServices.Guid("1A27878B-EE15-41CE-B427-58B10390C821"), System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)>
        Private Interface IVSDProjectProperties
        End Interface

        ''' <summary>
        ''' Wrapper class for IVsShell.OnBroadcastMessage
        ''' </summary>
        Friend Class BroadcastMessageEventsHelper
            Implements IVsBroadcastMessageEvents
            Implements IDisposable

            Public Event BroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr)

            'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
            Private _cookieBroadcastMessages As UInteger
            Private ReadOnly _serviceProvider As IServiceProvider

            Friend Sub New(sp As IServiceProvider)
                _serviceProvider = sp
                ConnectBroadcastEvents()
            End Sub

#Region "Helper methods to advise/unadvise broadcast messages from the IVsShell service"

            Friend Sub ConnectBroadcastEvents()
                Dim VSShell As IVsShell = Nothing
                If _serviceProvider IsNot Nothing Then
                    VSShell = DirectCast(_serviceProvider.GetService(GetType(IVsShell)), IVsShell)
                End If
                If VSShell IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(VSShell.AdviseBroadcastMessages(Me, _cookieBroadcastMessages))
                Else
                    Debug.Fail("Unable to get IVsShell for broadcast messages")
                End If
            End Sub

            Private Sub DisconnectBroadcastMessages()
                If _cookieBroadcastMessages <> 0 Then
                    Dim VsShell As IVsShell = DirectCast(_serviceProvider.GetService(GetType(IVsShell)), IVsShell)
                    If VsShell IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(VsShell.UnadviseBroadcastMessages(_cookieBroadcastMessages))
                        _cookieBroadcastMessages = 0
                    End If
                End If
            End Sub

#End Region

            ''' <summary>
            ''' Forward to overridable OnBroadcastMessage handler
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            Private Function IVsBroadcastMessageEvents_OnBroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr) As Integer Implements IVsBroadcastMessageEvents.OnBroadcastMessage
                OnBroadcastMessage(msg, wParam, lParam)
                Return Interop.NativeMethods.S_OK
            End Function

            ''' <summary>
            ''' Raise OnBroadcastMessage event. Can be overridden to implement custom handling of broadcast messages
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            Protected Overridable Sub OnBroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr)
                RaiseEvent BroadcastMessage(msg, wParam, lParam)
            End Sub

#Region "Standard dispose pattern - the only thing we need to do is to unadvise events..."

            Private _disposed As Boolean

            ' IDisposable
            Private Overloads Sub Dispose(disposing As Boolean)
                If Not _disposed Then
                    If disposing Then
                        DisconnectBroadcastMessages()
                    End If
                End If
                _disposed = True
            End Sub

#Region " IDisposable Support "
            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

            Protected Overrides Sub Finalize()
                ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
                Dispose(False)
                MyBase.Finalize()
            End Sub
#End Region
#End Region

        End Class

        ''' <summary>
        ''' Monitor and set font when font changes...
        ''' </summary>
        Friend NotInheritable Class FontChangeMonitor
            Inherits BroadcastMessageEventsHelper

            ' Control that we are going to set the font on (if any)
            Private ReadOnly _control As Control

            Private ReadOnly _serviceProvider As IServiceProvider

            ''' <summary>
            ''' Create a new instance...
            ''' </summary>
            ''' <param name="sp"></param>
            ''' <param name="ctrl"></param>
            ''' <param name="SetFontInitially">If true, set the font of the provided control when this FontChangeMonitor is created</param>
            Public Sub New(sp As IServiceProvider, ctrl As Control, SetFontInitially As Boolean)
                MyBase.New(sp)

                Debug.Assert(sp IsNot Nothing, "Why did we get a NULL service provider!?")
                Debug.Assert(ctrl IsNot Nothing, "Why didn't we get a control to provide the dialog font for!?")

                _serviceProvider = sp
                _control = ctrl

                If SetFontInitially Then
                    _control.Font = GetDialogFont(sp)
                End If
            End Sub

            ''' <summary>
            ''' Override to get WM_SETTINGCHANGE notifications and set the font accordingly...
            ''' </summary>
            ''' <param name="msg"></param>
            ''' <param name="wParam"></param>
            ''' <param name="lParam"></param>
            Protected Overrides Sub OnBroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr)
                MyBase.OnBroadcastMessage(msg, wParam, lParam)

                If _control IsNot Nothing Then
                    If msg = Interop.Win32Constant.WM_SETTINGCHANGE Then
                        ' Only set font if it is different from the current font...
                        Dim newFont As Font = GetDialogFont(_serviceProvider)
                        If Not newFont.Equals(_control.Font) Then
                            _control.Font = newFont
                        End If
                    End If
                End If
            End Sub

            ''' <summary>
            ''' Pick current dialog font...
            ''' </summary>
            Friend Shared ReadOnly Property GetDialogFont(ServiceProvider As IServiceProvider) As Font
                Get
                    If ServiceProvider IsNot Nothing Then
                        Dim uiSvc As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                        If uiSvc IsNot Nothing Then
                            Return CType(uiSvc.Styles("DialogFont"), Font)
                        End If
                    End If

                    Debug.Fail("Couldn't get a IUIService... cheating instead :)")

                    Return Control.DefaultFont
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Determine if the specified custom tool is registered for the current project system
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy to check if the custom tool is registered for</param>
        ''' <param name="customToolName">Name of custom tool to look for</param>
        ''' <returns>True if registered, false otherwise</returns>
        Friend Shared Function IsCustomToolRegistered(hierarchy As IVsHierarchy, customToolName As String) As Boolean
            Requires.NotNull(hierarchy, NameOf(hierarchy))
            Requires.NotNull(customToolName, NameOf(customToolName))

            ' All project systems support empty string (= no custom tool)
            If customToolName.Length = 0 Then Return True

            Dim sfgFactory As IVsSingleFileGeneratorFactory = TryCast(hierarchy, IVsSingleFileGeneratorFactory)
            If sfgFactory Is Nothing Then
                ' If the hierarchy doesn't support IVsSingleFileGeneratorFactory, then we assume that
                ' the custom tools aren't supported by the project system.
                Return False
            End If

            Dim pbGeneratesDesignTimeSource As Integer
            Dim pbGeneratesSharedDesignTimeSource As Integer
            Dim pbUseTempPEFlag As Integer
            Dim pguidGenerator As Guid

            Dim hr As Integer = sfgFactory.GetGeneratorInformation(customToolName, pbGeneratesDesignTimeSource, pbGeneratesSharedDesignTimeSource, pbUseTempPEFlag, pguidGenerator)

            If VSErrorHandler.Succeeded(hr) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Shared Function GetServiceProvider(dte As DTE) As IServiceProvider
            Return New Shell.ServiceProvider(DirectCast(dte, OLE.Interop.IServiceProvider))
        End Function
        ''' <summary>
        ''' VSHPROPID_IsDefaultNamespaceRefactorNotify only exists in C#.  Other langs will not have this property
        ''' 
        ''' C# does not support default namespace rename.  this flag will tell the caller if
        ''' this is a default renamespace rename or not.
        ''' </summary>
        ''' <param name="pHier"></param>
        ''' <param name="itemId"></param>
        Public Shared Function IsDefaultNamespaceRename(pHier As IVsHierarchy, itemId As UInteger) As Boolean
            ' result <<== out
            Dim result As Object = Nothing
            Dim success As Boolean = VSErrorHandler.Succeeded(pHier.GetProperty(itemId, CType(__VSHPROPID3.VSHPROPID_IsDefaultNamespaceRefactorNotify, Integer), result))

            If Not success OrElse result Is Nothing Then
                Return False
            End If

            Return CType(result, Boolean)
        End Function

        ''' <summary>
        ''' Create a Type Resolution Service.
        ''' </summary>
        ''' <param name="serviceProvider"></param>
        ''' <param name="hierarchy"></param>
        Friend Shared Function CreateTypeResolutionService(serviceProvider As IServiceProvider, hierarchy As IVsHierarchy) As System.ComponentModel.Design.ITypeResolutionService
            Dim dynamicTypeService As Shell.Design.DynamicTypeService =
                    TryCast(serviceProvider.GetService(
                    GetType(Shell.Design.DynamicTypeService)),
                    Shell.Design.DynamicTypeService)

            Dim trs As System.ComponentModel.Design.ITypeResolutionService = Nothing

            If dynamicTypeService IsNot Nothing Then
                trs = dynamicTypeService.GetTypeResolutionService(hierarchy, VSITEMID.ROOT)
            End If

            Return trs
        End Function

        ''' <summary>
        ''' Gets VS color from the shell's color service.  If for some reason this fails or <paramref name="UseVSTheme"/> is False, returns the supplied
        ''' default color.
        ''' </summary>
        ''' <param name="VsSysColorIndex"></param>
        ''' <param name="DefaultColor"></param>
        ''' <param name="UseVSTheme">Whether to use VS Shell to map the right color or just use the default one.</param>
        Public Shared Function GetVSColor(VsSysColorIndex As __VSSYSCOLOREX3, DefaultColor As Color, Optional UseVSTheme As Boolean = True) As Color
            If Not UseVSTheme Then
                Return DefaultColor
            End If
            ' VBPackage.Instance cannot be Nothing
            Dim VsUIShell2 As IVsUIShell2 = DirectCast(Shell.Package.GetGlobalService(GetType(SVsUIShell)), IVsUIShell2)

            If VsUIShell2 IsNot Nothing Then
                Dim abgrValue As UInteger
                Dim Hr As Integer = VsUIShell2.GetVSSysColorEx(VsSysColorIndex, abgrValue)
                If VSErrorHandler.Succeeded(Hr) Then
                    Return ColorTranslator.FromWin32(CType(abgrValue, Integer))
                End If
            End If

            Debug.Fail("Unable to get color from the shell, using a predetermined default color instead." & vbCrLf & "Color Index = " & VsSysColorIndex & ", Default Color = &h" & Hex(DefaultColor.ToArgb))
            Return DefaultColor
        End Function

    End Class

End Namespace
