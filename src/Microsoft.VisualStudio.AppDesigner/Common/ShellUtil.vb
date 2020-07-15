' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Imports EnvDTE

Imports Microsoft.VisualStudio.Shell.Interop

Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    ''' <summary>
    ''' Utilities relating to the Visual Studio shell, services, etc.
    ''' </summary>
    Friend NotInheritable Class ShellUtil

        Public Shared ReadOnly ProjectDesignerThemeCategory As New Guid("ef1a2d2c-5d16-4ddb-8d04-79d0f6c1c56e")

        Public Shared ReadOnly EnvironmentThemeCategory As New Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d")

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

        Public Shared Function GetDesignerThemeColor(uiShellService As IVsUIShell5, themeCategory As Guid, themeColorName As String, colorType As __THEMEDCOLORTYPE, defaultColor As Color) As Color

            If uiShellService IsNot Nothing Then
                Dim rgbaValue As UInteger

                Dim hr As Integer = VSErrorHandler.CallWithCOMConvention(
                    Sub()
                        rgbaValue = uiShellService.GetThemedColor(themeCategory, themeColorName, CType(colorType, UInteger))
                    End Sub)

                If VSErrorHandler.Succeeded(hr) Then
                    Return RGBAToColor(rgbaValue)
                End If
            End If

            Debug.Fail("Unable to get color from the shell, using a predetermined default color instead." & vbCrLf & "Color Category = " & themeCategory.ToString() & ", Color Name = " & themeColorName & ", Color Type = " & colorType & ", Default Color = &h" & Hex(defaultColor.ToArgb))
            Return defaultColor
        End Function

        Public Shared Function GetEnvironmentThemeColor(uiShellService As IVsUIShell5, themeColorName As String, colorType As __THEMEDCOLORTYPE, defaultColor As Color) As Color
            Return GetDesignerThemeColor(uiShellService, EnvironmentThemeCategory, themeColorName, colorType, defaultColor)
        End Function

        Public Shared Function GetProjectDesignerThemeColor(uiShellService As IVsUIShell5, themeColorName As String, colorType As __THEMEDCOLORTYPE, defaultColor As Color) As Color
            Return GetDesignerThemeColor(uiShellService, ProjectDesignerThemeCategory, themeColorName, colorType, defaultColor)
        End Function

        Private Shared Function RGBAToColor(rgbaValue As UInteger) As Color
            Return Color.FromArgb(CInt((rgbaValue And &HFF000000UI) >> 24), CInt(rgbaValue And &HFFUI), CInt((rgbaValue And &HFF00UI) >> 8), CInt((rgbaValue And &HFF0000UI) >> 16))
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
        ''' Wrapper class for IVsShell.OnBroadcastMessage
        ''' </summary>
        Public Class BroadcastMessageEventsHelper
            Implements IVsBroadcastMessageEvents
            Implements IDisposable

            Public Event BroadcastMessage(msg As UInteger, wParam As IntPtr, lParam As IntPtr)

            'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
            Private _cookieBroadcastMessages As UInteger
            Private ReadOnly _serviceProvider As IServiceProvider

            Public Sub New(sp As IServiceProvider)
                _serviceProvider = sp
                ConnectBroadcastEvents()
            End Sub

#Region "Helper methods to advise/unadvise broadcast messages from the IVsShell service"

            Public Sub ConnectBroadcastEvents()
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
                Return AppDesInterop.NativeMethods.S_OK
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
        Public NotInheritable Class FontChangeMonitor
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
                    If msg = AppDesInterop.Win32Constant.WM_SETTINGCHANGE Then
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
            Public Shared ReadOnly Property GetDialogFont(ServiceProvider As IServiceProvider) As Font
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

    End Class

End Namespace
