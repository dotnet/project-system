' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Microsoft.VisualStudio.Settings
Imports Microsoft.VisualStudio.Setup.Configuration
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Public NotInheritable Class GeneralOptions
        <Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")>
        Private Class SVsSettingsPersistenceManager
        End Class

        Private Const PreviewChannelPattern As String = "VisualStudio\.[0-9]+\.Preview/"
        Private Const UsePreviewSdkMarkerFileName As String = "sdk.txt"
        Private Const FastUpToDateEnabledSettingKey As String = "ManagedProjectSystem\FastUpToDateCheckEnabled"
        Private Const FastUpToDateLogLevelSettingKey As String = "ManagedProjectSystem\FastUpToDateLogLevel"
        Private Const UsePreviewSdkSettingKey As String = "ManagedProjectSystem\UsePreviewSdk"

        Private ReadOnly _settingsManager As ISettingsManager
        Private ReadOnly _shell As IVsShell
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
        ''' If there no marker file there, it measn VS is having default setting value which was 
        ''' never changed by user manually. Out-proc components should treat absense of the file as 
        ''' default option value and check the channel using same setup API we use here. 
        ''' This absense=default logic is needed for the case VS is installed, was never started yet,
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
            End Set
        End Property

        Public Sub New(serviceProvider As IServiceProvider)
            _settingsManager = CType(serviceProvider.GetService(GetType(SVsSettingsPersistenceManager)), ISettingsManager)
            _shell = CType(serviceProvider.GetService(GetType(SVsShell)), IVsShell)

            Dim oPath As Object = Nothing
            If (Not VSErrorHandler.Failed(_shell.GetProperty(__VSSPROPID4.VSSPROPID_LocalAppDataDir, oPath))) And
                (oPath IsNot Nothing) And
                (TypeOf oPath Is String) Then
                _localAppData = CType(oPath, String)
            End If

            _isPreview = IsPreview()
        End Sub

        ''' <summary>
        ''' Uses VS setup API, to determine if current VS instance came from Preview channel or not.
        ''' Note: we depend here on channel manifest format. If it changes, we could be broken. There is
        ''' an integration test in WTE repo to be executed in nightly vendors' runs to protect form manifest 
        ''' format changes.
        ''' </summary>
        ''' <returns></returns>
        Private Function IsPreview() As Boolean
            Dim vsSetupConfig = New SetupConfiguration()
            Dim installedInstances As List(Of ISetupInstance) = New List(Of ISetupInstance)()
            Dim installationPath As Object = Nothing

            If (Not VSErrorHandler.Failed(_shell.GetProperty(__VSSPROPID.VSSPROPID_InstallDirectory, installationPath))) AndAlso
                (installationPath IsNot Nothing) AndAlso
                (TypeOf installationPath Is String) AndAlso
                (Not String.IsNullOrEmpty(installationPath)) Then

                Dim instances = vsSetupConfig.EnumAllInstances()
                Dim foundInstance As ISetupInstance2 = Nothing
                Dim instanceBuffer() As ISetupInstance = New ISetupInstance(0) {}
                Dim fetched As Integer

                Do
                    fetched = 0
                    instances.Next(1, instanceBuffer, fetched)

                    If ((fetched > 0) AndAlso
                        installationPath.StartsWith(instanceBuffer(0).GetInstallationPath() + "\", StringComparison.OrdinalIgnoreCase)) Then
                        foundInstance = CType(instanceBuffer(0), ISetupInstance2)
                    End If
                Loop While ((fetched > 0) And (foundInstance Is Nothing))

                If (foundInstance IsNot Nothing) Then
                    ' Manifest takes form of: "VisualStudio.15.Preview/15.8.0-pre.2.0+27714.3000.d15.8stg"
                    Dim channelManifest = foundInstance.GetProperties().GetValue("channelManifestId").ToString()
                    Return Regex.IsMatch(channelManifest, PreviewChannelPattern, RegexOptions.IgnoreCase)
                End If
            End If

            Return False
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
                Catch ex As IOException
                    attempts = attempts - 1
                End Try
            Loop
        End Sub
    End Class
End Namespace
