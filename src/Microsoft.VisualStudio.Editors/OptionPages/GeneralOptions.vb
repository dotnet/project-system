' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.IO
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Settings
Imports Microsoft.VisualStudio.Setup.Configuration
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Public NotInheritable Class GeneralOptions
        <Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")>
        Private Class SVsSettingsPersistenceManager
        End Class

        Private Const UsePreviewSdkMarkerFileName As String = "sdk.txt"
        Private Const FastUpToDateEnabledSettingKey As String = "ManagedProjectSystem\FastUpToDateCheckEnabled"
        Private Const FastUpToDateLogLevelSettingKey As String = "ManagedProjectSystem\FastUpToDateLogLevel"
        Private Const UsePreviewSdkSettingKey As String = "ManagedProjectSystem\UsePreviewSdk"

        Private ReadOnly _settingsManager As ISettingsManager
        Private ReadOnly _localAppData As String
        Private ReadOnly _isPreview As Boolean

        Public Property FastUpToDateCheckEnabled As Boolean
            Get
                Return If(_settingsManager?.GetValueOrDefault(FastUpToDateEnabledSettingKey, True), True)
            End Get
            Set
                _settingsManager.SetValueAsync(FastUpToDateEnabledSettingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        Public Property FastUpToDateLogLevel As LogLevel
            Get
                Return If(_settingsManager?.GetValueOrDefault(FastUpToDateLogLevelSettingKey, LogLevel.None), LogLevel.None)
            End Get
            Set
                _settingsManager.SetValueAsync(FastUpToDateLogLevelSettingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        ''' <summary>
        ''' This setting has dynamic default value:
        '''     - true if VS was installed from Preview channel
        '''     - false if VS was installed from Release channel.
        ''' When default setting value is changed by user, we write current value to a marker file 
        ''' located under local AppData VS instance folder to be used by out-proc components.
        ''' If there no marker file there, it means VS is having default setting value which was 
        ''' never changed by user manually. Out-proc components should treat absence of the file as 
        ''' default option value and check the channel using same setup API we use here. 
        ''' This absence=default logic is needed for the case VS is installed, was never started yet,
        ''' but user runs msbuild form dev command line to build solution. In this case msbuild 
        ''' components should have a way to figure out default setting on their own.
        ''' 
        ''' Note: This setting is not roamed. After chatting with shell team, we decided not to roam 
        ''' it due to stability issues.
        ''' </summary>
        ''' <returns></returns>
        Public Property UsePreviewSdk As Boolean
            Get
                Return If(_settingsManager?.GetValueOrDefault(UsePreviewSdkSettingKey, _isPreview), _isPreview)
            End Get
            Set
                _settingsManager.SetValueAsync(UsePreviewSdkSettingKey, Value, isMachineLocal:=False)

                UpdateSdkMarkerFile(Value)
                TelemetryLogger.LogUsePreviewSdkEvent(Value, _isPreview)
            End Set
        End Property

        Public ReadOnly Property CanChangeUsePreviewSdk As Boolean
            Get
                Return Not _isPreview
            End Get
        End Property

        Public Sub New(serviceProvider As IServiceProvider)
            _settingsManager = DirectCast(serviceProvider.GetService(GetType(SVsSettingsPersistenceManager)), ISettingsManager)
            Dim shell = DirectCast(serviceProvider.GetService(GetType(SVsShell)), IVsShell)

            Dim oPath As Object = Nothing
            If (Not VSErrorHandler.Failed(shell.GetProperty(__VSSPROPID4.VSSPROPID_LocalAppDataDir, oPath))) And
                (oPath IsNot Nothing) And
                (TypeOf oPath Is String) Then
                _localAppData = DirectCast(oPath, String)
            End If

            _isPreview = IsPreview()
        End Sub

        ''' <summary>
        ''' Uses VS setup API, to determine if current VS instance came from Preview channel or not.
        ''' </summary>
        ''' <returns></returns>
        Private Shared Function IsPreview() As Boolean
            Dim vsSetupConfig = New SetupConfiguration()
            Dim setupInstance = vsSetupConfig.GetInstanceForCurrentProcess()
            Dim setupInstanceCatalog = DirectCast(setupInstance, ISetupInstanceCatalog)

            ' Release: false. Others: true.
            Return setupInstanceCatalog.IsPrerelease()
        End Function

        ''' <summary>
        ''' This method creates a marker file containing a value for UsePreviewSdk setting to be used 
        ''' out-proc (msbuild Sdk Resolver). Marker file is stored into current instance folder under 
        ''' local AppData.
        ''' </summary>
        ''' <param name="usePreviews"></param>
        Private Sub UpdateSdkMarkerFile(usePreviews As Boolean)
            If (String.IsNullOrEmpty(_localAppData)) Then
                Return
            End If

            Dim sdkTxtPath As String = Path.Combine(_localAppData, UsePreviewSdkMarkerFileName)
            Dim attempts As Integer = 3
            Do While (attempts > 0)
                Try
                    File.WriteAllText(sdkTxtPath, $"UsePreviews={usePreviews}")
                    attempts = 0
                Catch ex As Exception
                    attempts -= 1
                End Try
            Loop
        End Sub
    End Class
End Namespace
